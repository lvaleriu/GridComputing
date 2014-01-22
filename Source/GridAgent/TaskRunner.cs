#region

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Remoting;
using System.Threading;
using System.Threading.Tasks;
using GridAgent.Concurrency;
using GridAgentSharedLib;
using GridAgentSharedLib.Clients;
using GridAgentSharedLib.TypesCreation;
using GridComputingSharedLib;
using GridSharedLibs;
using GridSharedLibs.ServiceModel.Types;
using ServiceStack.Logging;
using ServiceStack.ServiceClient.Web;
using File = System.IO.File;

#endregion

namespace GridAgent
{
    /// <summary>
    ///     Manages and provides an interface for a single task.
    /// </summary>
    public class TaskRunner : MarshalByRefObject, ITaskRunner
    {
        #region Fields

        private const string TasksCompletedSetting = "TasksCompleted";

        private const int UpdateProgressFrequencySec = 20;
        private const int PingFrequencySec = 10;

        private readonly Config _config;
        private readonly Timer _getJobtimer;
        private readonly object _historyLock = new object();
        private readonly IGlobalService _instance;

        private readonly Dictionary<string, Dictionary<string, string>> _loadedAssemblies = new Dictionary<string, Dictionary<string, string>>();
        private readonly Queue<AppDomain> _loadedDomains = new Queue<AppDomain>();
        private readonly ILog _log = LogManager.GetLogger(typeof (TaskRunner));
        private readonly Timer _pingTimer;
        private readonly Statistics _statistics;
        private readonly ConcurrentDictionary<string, TaskBenchmark> _taskBenchmarks;

        private readonly object _updatingJobProgressLock = new object();
        private Agent _agent;
        private Guid _agentId = Guid.Empty;
        private TaskInformation _previousTaskInfo;
        private int _tasksCompleted;
        private volatile bool _updatingJobProgress;
        private bool _working;
        private WrapperSlaveClass _wrapperSlaveClass;

        private class TaskInformation
        {
            public TaskDescriptor Descriptor { get; set; }
            public ICreateSlaveInstance CreateSlaveInstance { get; set; }
            public InstanceCreatorType CreatorType { get; set; }
        }

        #endregion

        public TaskRunner(Config config, IGlobalService iGlobalService)
        {
            _instance = iGlobalService;
            _config = config;

            _statistics = new Statistics(_instance);

            /* Prompt the ThreadSynchronizer to start 
             * monitoring for callbacks to the main thread. */
            ThreadSynchronizer.SetContext(null);

            _taskBenchmarks = new ConcurrentDictionary<string, TaskBenchmark>();
            List<TaskBenchmark> benchmarks;
            if (IsolatedStorageUtility.TryDeserialize("TaskBenchmark", out benchmarks))
            {
                benchmarks.ForEach(e => _taskBenchmarks.TryAdd(e.RepositoryName + e.TaskName, e));
            }

            _tasksCompleted = GetTasksCompleted();

            _getJobtimer = new Timer(OnTimerCallback, null, 1000, UpdateProgressFrequencySec*1000);
            _pingTimer = new Timer(OnPingTimerCallback, null, 1000, PingFrequencySec*1000);
        }

        public static bool EnableMasterCreatorsPing { get; set; }

        /// <summary>
        ///     Gets the owner <see cref="Client" /> 's id.
        /// </summary>
        /// <value> The client's id. </value>
        public Guid ClientId
        {
            get
            {
                if (_agentId == Guid.Empty)
                    Register();
                return _agentId;
            }
        }

        /// <summary>
        ///     Gets the name of the task.
        /// </summary>
        /// <value> The name of the task. </value>
        public string TaskName
        {
            get
            {
                if (_wrapperSlaveClass != null && _wrapperSlaveClass.Descriptor != null && _wrapperSlaveClass.Descriptor.Job != null)
                {
                    return _wrapperSlaveClass.Descriptor.Job.TaskName;
                }
                return string.Empty;
            }
        }

        /// <summary>
        ///     Gets the tasks completed by the <see cref="Agent" /> .
        /// </summary>
        /// <value> The tasks completed by the agent. </value>
        public int TasksCompleted
        {
            get { return _tasksCompleted; }
        }

        public void Start()
        {
            if (State != TaskRunnerState.Started)
            {
                State = TaskRunnerState.Started;
                try
                {
                    lock (this)
                    {
                        if (_agentId == Guid.Empty)
                            Register();
                    }
                    LoadAndRunTask();
                }
                catch (Exception)
                {
                    State = TaskRunnerState.Stopped;
                    throw;
                }
            }
        }

        public void Stop()
        {
            _getJobtimer.Change(Timeout.Infinite, Timeout.Infinite);
            _pingTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        #region TaskRunnerState

        private TaskRunnerState _state = TaskRunnerState.Stopped;

        /// <summary>
        ///     Gets or sets the state of the task runner. This indicates what is being done with the <see cref="SlaveTask" />
        ///     instance.
        /// </summary>
        /// <value> The state of the task runner. </value>
        public TaskRunnerState State
        {
            get { return _state; }
            private set { _state = value; }
        }

        #endregion

        #region Progress

        /// <summary>
        ///     Gets the steps completed by the <see cref="SlaveTask" /> .
        /// </summary>
        /// <value> The steps completed by the task. </value>
        public long StepsCompleted
        {
            get
            {
                if (_wrapperSlaveClass != null)
                    return _wrapperSlaveClass.StepsCompleted;
                return 0;
            }
        }

        /// <summary>
        ///     Gets the steps goal of the <see cref="SlaveTask" /> .
        /// </summary>
        /// <value> The steps goal of the task. </value>
        public long StepsGoal
        {
            get
            {
                if (_wrapperSlaveClass != null)
                    return _wrapperSlaveClass.StepsGoal;
                return 0;
            }
        }

        #endregion

        public override object InitializeLifetimeService()
        {
            return null;
        }

        #region Private methods

        private void Register()
        {
            var gridService = _instance;
            var agent = CreateAgent();

            try
            {
                _agentId = gridService.Register(agent);
                agent.Id = _agentId;
            }
            catch (Exception ex)
            {
                throw new TaskException("Unable to connect to Grid Service.", ex);
            }
        }

        private Agent CreateAgent()
        {
            return _agent ?? (_agent = new Agent
            {
                Id = _agentId,
                MFlops = _statistics.GetMFlops(),
                BandwidthKBps = _statistics.GetBandwidthKBs(),
                MachineName = Environment.MachineName,
                UserName = Environment.UserName,
                ProcessorCount = Environment.ProcessorCount,
                TotalPhysicalMemory = _statistics.GetTotalPhysicalMemoryKBs(),
                IPAddress = GetIpAddress(),
            });
        }

        private string GetIpAddress()
        {
            string localIp = "?";
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIp = ip.ToString();
                }
            }
            return localIp;
        }

        private void OnPingTimerCallback(object state)
        {
            try
            {
                lock (this)
                {
                    if (_agentId == Guid.Empty)
                        return;
                }

                _instance.Ping(CreateAgent());
            }
            catch (Exception ex)
            {
                _log.Error("OnPingTimerCallback", ex);
            }
        }

        private void OnTimerCallback(object state)
        {
            TimerTick();
        }

        private int GetTasksCompleted()
        {
            int result = 0;
            //lock (_historyLock)
            //{
            //    object temp;
            //    if (IsolatedStorageUtility.TryGetSetting(TasksCompletedSetting, out temp))
            //    {
            //        try
            //        {
            //            result = int.Parse(temp.ToString());
            //        }
            //        catch (FormatException ex)
            //        {
            //            _log.Error("TasksCompleted saved in invalid format as: " + temp, ex);
            //        }
            //    }
            //}
            return result;
        }

        private void IncrementTasksCompleted()
        {
            return;

            lock (_historyLock)
            {
                _tasksCompleted = GetTasksCompleted();
                try
                {
                    IsolatedStorageUtility.SaveSetting(TasksCompletedSetting, ++_tasksCompleted);
                }
                catch (IOException ex)
                {
                    _log.Warn("Unable to save setting: " + TasksCompletedSetting
                              + " Possibly a race condition due to multiple browsers open?", ex);
                }
                catch (Exception ex)
                {
                    _log.Warn("Unable to save setting: " + TasksCompletedSetting, ex);
                }
            }
        }

        /// <summary>
        ///     Loads the and runs a task.
        /// </summary>
        /// <exception cref="TaskException" />
        private void LoadAndRunTask()
        {
            // Channel factory initialisation
            var gridService = _instance;

            var agent = CreateAgent();

            TaskDescriptor descriptor;
            try
            {
                descriptor = gridService.StartNewJob(agent);
            }
            catch (Exception ex)
            {
                throw new TaskException("Unable to LoadAndRunTask.", ex);
            }

            if (descriptor == null)
                throw new TaskException("StartNewJob web service call returned a null descriptor.");

            if (!descriptor.Enabled)
            {
                _log.Info("No job is available");
                new Action(() =>
                {
                    Thread.Sleep(1000);
                    RetryTaskStart();
                }).BeginInvoke(null, null);
                return;
            }

            LoadAndRunTaskAux(descriptor);

            _getJobtimer.Change(1000, UpdateProgressFrequencySec*1000);
        }

        private void RetryTaskStart()
        {
            while (true)
            {
                try
                {
                    LoadAndRunTask();
                    return;
                }
                catch (Exception ex)
                {
                    _log.Error("Problem with RetryTaskStart with no delay.", ex);
                }
                Thread.Sleep(1000);
            }
        }

        private void LoadAndRunTaskAux(TaskDescriptor descriptor)
        {
            if (_previousTaskInfo != null && _previousTaskInfo.CreatorType != InstanceCreatorType.RemoteProxy)
            {
                if (true)
                {
                    if (_loadedDomains.Count > 1)
                    {
                        AppDomain.Unload(_loadedDomains.Dequeue());
                    }
                }
                else
                {
                    var domains = LibTools.GetAppDomains();

                    if (domains.Count() > 2)
                    {
                        AppDomain.Unload(domains[1]);
                    }
                }
            }

            _log.Debug("LoadAndRunTask() for Job Id " + descriptor.Job.Id);

            /* Get the assembly name to download. */
            string assemblyQualifiedName = descriptor.TypeAssemblyName;
            string assemblyName;
            if (!TryGetDownloadName(assemblyQualifiedName, out assemblyName))
                throw new TaskException("Unable to extract assembly name from string " + assemblyQualifiedName);

            Task.Factory.StartNew(() =>
            {
                if (_previousTaskInfo != null && _previousTaskInfo.Descriptor.Id == descriptor.Id)
                {
                    _previousTaskInfo.Descriptor = descriptor;
                    ReuseSlaveTask(assemblyName, _previousTaskInfo);
                }
                else
                    GetWork(assemblyName, descriptor);
            });
        }

        private void ReuseSlaveTask(string assemblyName, TaskInformation taskInformation)
        {
            try
            {
                OnAssemblyOpened(taskInformation, _wrapperSlaveClass.SlaveTask, true);
            }
            catch (Exception ex)
            {
                _log.Error("Unable to load work: " + ex.StackTrace, ex);

                if (ex.InnerException != null)
                    _log.Error("Inner exception: ", ex.InnerException);

                CancelJob(taskInformation.Descriptor);

                OnTaskError(new SEventArgs
                {
                    Message = string.Format("Unable to load work: {0} ", assemblyName)
                });
            }
        }

        private void GetWork(string assemblyName, TaskDescriptor descriptor)
        {
            TaskInformation taskInformation = null;
            try
            {
                string taskDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _config.SlaveTasksFolder, descriptor.Job.TaskName);
                string repDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _config.RepositoryTasksFolder, descriptor.Job.TaskName);
                string dllLocation = Path.Combine(taskDirectory, assemblyName);

                taskInformation = GetNewTaskInformation(descriptor, taskDirectory, repDirectory, _previousTaskInfo);

                // Use 32Bit check to start the right process
                ISlaveTask task = taskInformation.CreateSlaveInstance.CreateSlaveTask(descriptor.TypeName, dllLocation);

                if (task != null)
                {
                    OnAssemblyOpened(taskInformation, task, false);
                }
                else
                {
                    string message = string.Format("Cant load task " + descriptor.TypeName);

                    HandleGetWorkException(descriptor, new Exception(message), taskInformation);
                }
            }
            catch (RemotingException ex)
            {
                HandleGetWorkException(descriptor, ex, taskInformation);
            }
            catch (SocketException ex)
            {
                HandleGetWorkException(descriptor, ex, taskInformation);
            }
            catch (WebServiceException ex)
            {
                HandleGetWorkException(descriptor, ex, taskInformation);
            }
            catch (WebException ex)
            {
                HandleGetWorkException(descriptor, ex, taskInformation);
            }
            catch (Exception ex)
            {
                HandleGetWorkException(descriptor, ex, taskInformation);
            }
        }

        private void HandleGetWorkException(TaskDescriptor descriptor, Exception ex, TaskInformation taskInformation)
        {
            string message = string.Format("Lost connexion with the slave launcher process. Error: {0}", GridLog.SerializeException(ex));

            _log.Error(message, ex);

            CancelJob(descriptor);

            if (taskInformation != null && taskInformation.CreateSlaveInstance != null)
                taskInformation.CreateSlaveInstance.Close();

            OnTaskError(new SEventArgs
            {
                Message = message,
            });
        }

        private TaskInformation GetNewTaskInformation(TaskDescriptor descriptor, string tasksDirectory, string repositoryDirectory, TaskInformation previousTaskInformation)
        {
            var repositoryTaskUpdatesMapping = new Dictionary<string, string>();

            bool differentDlls = DownloadTaskFiles(descriptor, tasksDirectory, repositoryDirectory, repositoryTaskUpdatesMapping);
            bool differentTask;
            ICreateSlaveInstance createSlaveInstance;
            InstanceCreatorType creatorType;

            if (previousTaskInformation == null)
            {
                differentTask = true;
                createSlaveInstance = null;
                creatorType = InstanceCreatorType.RemoteProxy;
            }
            else
            {
                differentTask = (previousTaskInformation.Descriptor.Job.TaskName != descriptor.Job.TaskName);
                createSlaveInstance = previousTaskInformation.CreateSlaveInstance;
                creatorType = previousTaskInformation.CreatorType;
            }

            if (differentTask || differentDlls)
            {
                creatorType = GetCreationType(descriptor);

                if (creatorType == InstanceCreatorType.RemoteProxy)
                {
                    if (createSlaveInstance != null)
                        createSlaveInstance.Close();

                    createSlaveInstance = new RemoteServerConnector("AppHost", useIpcChannel: true, isClient: true, enableMasterCreatorsPing: EnableMasterCreatorsPing);
                }

                if (creatorType == InstanceCreatorType.NewAppDomainProxy)
                {
                    createSlaveInstance = new SlaveAppDomainCreateInstantiator<SlaveTask>("SlaveTask", tasksDirectory);
                    // TODO Should move this loading domains logic into the internal implementation
                    _loadedDomains.Enqueue(createSlaveInstance.GetExecutingDomain());
                }

                foreach (var mapping in repositoryTaskUpdatesMapping)
                {
                    var repTaskFileName = mapping.Key;
                    var taskfileName = mapping.Value;

                    File.Copy(repTaskFileName, taskfileName, true);
                }
            }

            return new TaskInformation {CreateSlaveInstance = createSlaveInstance, Descriptor = descriptor, CreatorType = creatorType};
        }

        private bool DownloadTaskFiles(TaskDescriptor descriptor, string slaveDir, string repoDir, Dictionary<string, string> repositoryTaskUpdatesMapping)
        {
            if (!Directory.Exists(slaveDir))
                Directory.CreateDirectory(slaveDir);

            if (!Directory.Exists(repoDir))
                Directory.CreateDirectory(repoDir);

            var loadedAssemblies = new Dictionary<string, string>();
            if (_loadedAssemblies.ContainsKey(descriptor.Job.TaskName))
            {
                loadedAssemblies = _loadedAssemblies[descriptor.Job.TaskName];
            }
            else
            {
                _loadedAssemblies[descriptor.Job.TaskName] = loadedAssemblies;
            }

            var response = _instance.GetFiles(descriptor.Job.TaskName + @"\Slave");
            var client = new WebClient();

            bool differentDlls = DownloadFolderFiles(loadedAssemblies, response.Directory, slaveDir, repoDir, descriptor, client, repositoryTaskUpdatesMapping);
            foreach (var folder in response.Directory.Folders)
            {
                string taskFolderPath = Path.Combine(slaveDir, folder.Name);
                string repositoryFolderPath = Path.Combine(repoDir, folder.Name);

                if (!Directory.Exists(taskFolderPath))
                    Directory.CreateDirectory(taskFolderPath);

                if (!Directory.Exists(repositoryFolderPath))
                    Directory.CreateDirectory(repositoryFolderPath);

                var folderResponse = _instance.GetFiles(descriptor.Job.TaskName + @"\Slave\" + folder.Name);

                differentDlls = differentDlls || DownloadFolderFiles(loadedAssemblies, folderResponse.Directory, taskFolderPath, repositoryFolderPath, descriptor, client, repositoryTaskUpdatesMapping);
            }

            return differentDlls;
        }

        private bool DownloadFolderFiles(Dictionary<string, string> loadedAssemblies,
            FolderResult directory,
            string folderPath,
            string repositoryFolderPath,
            TaskDescriptor descriptor,
            WebClient client,
            Dictionary<string, string> repositoryTaskUpdatesMapping)
        {
            bool differentDlls = false;

            foreach (var file in directory.Files)
            {
                string taskfileName = folderPath + @"\" + file.Name;
                string repTaskFileName = repositoryFolderPath + @"\" + file.Name;

                bool differentChecksum = false;
                if (loadedAssemblies.ContainsKey(taskfileName))
                {
                    differentChecksum = loadedAssemblies[taskfileName] != file.Checksum;
                }

                if (!_loadedAssemblies.ContainsKey(taskfileName) || differentChecksum)
                {
                    if (differentChecksum)
                        differentDlls = true;

                    if (File.Exists(taskfileName))
                    {
                        string checksum = Utils.GetMd5HashFromFile(taskfileName);
                        if (checksum == file.Checksum)
                        {
                            loadedAssemblies[taskfileName] = file.Checksum;
                            continue;
                        }

                        differentDlls = true;
                    }

                    //TODO Check if the file exists in all the repository folders (implemented tasks could share some libraries)
                    // When checking the other repositories, if we find one updated dll, we shall close the slave executing process before copying it
                    if (File.Exists(repTaskFileName))
                    {
                        string checksum = Utils.GetMd5HashFromFile(repTaskFileName);
                        if (checksum == file.Checksum)
                        {
                            File.Copy(repTaskFileName, taskfileName, true);
                            loadedAssemblies[taskfileName] = file.Checksum;
                            continue;
                        }

                        differentDlls = true;
                    }

                    int endIndexOfTaskName = folderPath.IndexOf(descriptor.Job.TaskName) + descriptor.Job.TaskName.Length;
                    string subFolderName = folderPath.Substring(endIndexOfTaskName, folderPath.Length - endIndexOfTaskName);
                    subFolderName = subFolderName.Replace("//", "");

                    string dirName = "Slave" + (string.IsNullOrWhiteSpace(subFolderName) ? "" : "/" + subFolderName);

                    var fileUri = new Uri(string.Format("{0}files/{1}/{2}/{3}?ForDownload=true", _config.UrlBase, descriptor.Job.TaskName, dirName, file.Name));
                    _log.Info(string.Format("Downloading file {1} to {0}", taskfileName, fileUri.OriginalString));

                    client.DownloadFile(fileUri, repTaskFileName);
                    repositoryTaskUpdatesMapping[repTaskFileName] = taskfileName;
                    loadedAssemblies[taskfileName] = file.Checksum;
                }
            }

            return differentDlls;
        }

        private InstanceCreatorType GetCreationType(TaskDescriptor descriptor)
        {
            var instanceCreatorType = InstanceCreatorType.RemoteProxy;
            var taskExecInfo = _instance.GetGridTasks(descriptor.Job.TaskName);
            var slaveTasks = taskExecInfo.Tasks.Where(t => t.Type == TaskType.Slave).ToList();

            if (slaveTasks.Any())
            {
                instanceCreatorType = slaveTasks.First().CreatorType;
            }

            return instanceCreatorType;
        }

        private void CancelJob(TaskDescriptor descriptor)
        {
            _working = false;
            try
            {
                _log.Info(string.Format("Cancelling job {0} {1}", descriptor.Job.TaskName, descriptor.Job.Id));
                var agent = CreateAgent();
                var information = new GridSharedLibs.TaskInformation
                {
                    JobId = descriptor.Job.Id,
                    TaskId = descriptor.Id,
                    TaskName = descriptor.Job.TaskName,
                };

                _instance.CancelJob(agent, information);
            }
            catch (Exception ex)
            {
                _log.Info(string.Format("Cancelling job {0} {1} failed: {2}", descriptor.Job.TaskName, descriptor.Job.Id, ex.Message));
            }
        }

        private bool TryGetDownloadName(string assemblyQualifiedName, out string assemblyName)
        {
            var splits = assemblyQualifiedName.Split(new[] {','});
            assemblyName = splits[0] + ".dll";
            return true;
        }

        private void SaveTaskBenchmark(TaskBenchmark benchmark)
        {
            string key = benchmark.RepositoryName + benchmark.TaskName;

            if (!_taskBenchmarks.ContainsKey(key))
                _taskBenchmarks.TryAdd(key, benchmark);
            else
            {
                TaskBenchmark existingBenchmark;
                if (_taskBenchmarks.TryGetValue(key, out existingBenchmark))
                {
                    _taskBenchmarks[key] = benchmark;
                }
            }

            IsolatedStorageUtility.TrySerialize("TaskBenchmark", _taskBenchmarks.Values.ToList());
        }

        #endregion

        #region Events

        private void OnAssemblyOpened(TaskInformation taskInformation, ISlaveTask task, bool reuseTask)
        {
            TaskDescriptor descriptor = taskInformation.Descriptor;

            if (!reuseTask)
            {
                //Create an instance of wrapper class.
                _wrapperSlaveClass = new WrapperSlaveClass(task);
            }

            _wrapperSlaveClass.Initialise(descriptor);
            _previousTaskInfo = taskInformation;

            OnTaskChanged(EventArgs.Empty);

            try
            {
                _working = true;

                //int procId = taskInformation.CreateSlaveInstance.GetProcessId();

                BenchmarkStatistics benchmarkStatistics;

                //using (benchmarkStatistics = new BenchmarkStatistics(procId))
                {
                    _wrapperSlaveClass.RunInternal();
                }

                var benchmark = new TaskBenchmark
                {
                    RepositoryName = descriptor.Job.TaskName,
                    TaskName = descriptor.TypeName,
                    //Duration = benchmarkStatistics.ElapsedTime,
                    //CpuUsagePercentage = benchmarkStatistics.ProcCpuUsage,
                };

                SaveTaskBenchmark(benchmark);

                OnTaskComplete();
            }
            catch (Exception ex)
            {
                _log.Warn(string.Format("RunInternal task failed! Ex: {0}", GridLog.SerializeException(ex)));
                CancelJob(_wrapperSlaveClass.Descriptor);

                OnTaskError(new SEventArgs {Message = string.Format("Error on running task : {0}", ex.Message)});
            }
        }

        private void OnTaskComplete()
        {
            try
            {
                _working = false;
                var result = new TaskResult
                {
                    Result = _wrapperSlaveClass.Result,
                    TaskId = _wrapperSlaveClass.Descriptor.Id,
                };

                if (_wrapperSlaveClass.Descriptor.Job != null)
                {
                    result.JobId = _wrapperSlaveClass.Descriptor.Job.Id;
                    result.TaskName = _wrapperSlaveClass.Descriptor.Job.TaskName;
                }

                _log.Debug(string.Format("Job {0} complete. Joining results ...", result.JobId));

                var gridService = _instance;
                var agent = CreateAgent();

                try
                {
                    if (gridService.JoinTask(agent, result) == -1)
                    {
                        _log.Warn("JoinTask failed!");
                        CancelJob(_wrapperSlaveClass.Descriptor);
                    }
                }
                catch (Exception ex)
                {
                    throw new TaskException("Unable to complete job: " + result.JobId, ex);
                }

                IncrementTasksCompleted();
                OnTaskComplete(EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _log.Error("OnTaskComplete raised exception." + ex.Message + " " + ex.StackTrace);
            }

            if (State == TaskRunnerState.Started)
                RetryTaskStart();
        }

        private void TimerTick()
        {
            lock (_updatingJobProgressLock)
            {
                if (_wrapperSlaveClass == null || _updatingJobProgress || !_working)
                    return;

                _updatingJobProgress = true;
            }

            try
            {
                UpdateJobProgressAux();
            }
            catch (Exception ex)
            {
                _log.Error("Problem calling ThreadPool.QueueUserWorkItem. Unsetting updatingJobProgress flag.", ex);
                _updatingJobProgress = false;
            }
        }

        private void UpdateJobProgressAux()
        {
            try
            {
                long workCompleted = _wrapperSlaveClass.StepsCompleted;
                long workGoal = _wrapperSlaveClass.StepsGoal;
                var progress = new TaskProgress
                {
                    StepsCompleted = workCompleted,
                    StepsGoal = workGoal,
                    TaskName = _wrapperSlaveClass.Descriptor.Job.TaskName,
                    TaskId = _wrapperSlaveClass.Descriptor.Id,
                };

                var gridService = _instance;
                try
                {
                    gridService.UpdateJobProgress(CreateAgent(), progress);
                }
                catch (Exception ex)
                {
                    _log.Error("Unable to update job progress.", ex);
                }
            }
            finally
            {
                _updatingJobProgress = false;
            }
        }

        #endregion

        #region event TaskChanged

        public event EventHandler TaskChanged
        {
            add { _taskChanged += value; }
            remove { _taskChanged -= value; }
        }

        private event EventHandler _taskChanged;

        private void OnTaskChanged(EventArgs e)
        {
            if (_taskChanged != null)
                _taskChanged(null, e);
        }

        #endregion

        #region event TaskError

        public event EventHandler<SEventArgs> TaskError
        {
            add { _taskError += value; }
            remove { _taskError -= value; }
        }

        private event EventHandler<SEventArgs> _taskError;

        private void OnTaskError(SEventArgs e)
        {
            State = TaskRunnerState.Stopped;

            if (_taskError != null)
                _taskError(null, e);
        }

        #endregion

        #region event TaskComplete

        /// <summary>
        ///     Occurs when the current task is complete.
        /// </summary>
        public event EventHandler TaskComplete
        {
            add { taskComplete += value; }
            remove { taskComplete -= value; }
        }

        private event EventHandler taskComplete;

        private void OnTaskComplete(EventArgs e)
        {
            if (taskComplete != null)
                taskComplete(null, e);
        }

        #endregion
    }
}