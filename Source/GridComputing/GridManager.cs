#region

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using GridAgentSharedLib;
using GridAgentSharedLib.Clients;
using GridAgentSharedLib.TypesCreation;
using GridComputing.Collections;
using GridComputing.Configuration;
using GridComputing.JobDistributions;
using GridComputingSharedLib;
using GridComputingSharedLib.TypesCreation;
using GridSharedLibs;
using Newtonsoft.Json;
using Quartz;
using Quartz.Impl;
using Quartz.Impl.Matchers;

#endregion

namespace GridComputing
{
    /// <summary>
    ///     Provides most external services for <see cref="Agent" />s
    ///     and managing <see cref="Client" />s. It is a mediator
    ///     for <see cref="MasterTask" />s and <see cref="Agent" />s.
    /// </summary>
    public class GridManager
    {
        #region Fields

        private readonly ExpiringDictionary<IAgent, TaskProgress> _agentsProgress;
        private readonly ExpiringDictionary<IAgent, short> _agentsPing;
        private readonly ExpiringDictionary<string, ICreateMasterInstance> _expiringCreatorsPerRepository;
        private readonly List<string> _ignoreExpirationRepositories = new List<string>();
        private readonly ConcurrentDictionary<string, InstanceLive> _instanceCreatorsLives;
        private readonly ConcurrentDictionary<string, ICreateMasterInstance> _instanceCreatorsPerRepository;
        private readonly IJobDistribution _jobDistributor;
        private readonly ConcurrentDictionary<string, GridTask> _loadedGridTaskElements;
        private readonly object _lockRemoval = new object();
        private readonly object _lockRunningTasksPerRepository = new object();
        private readonly IGridLog _log = GridLogManager.GetLogger(typeof (GridManager));
        private readonly Dictionary<Guid, MasterTaskEventHandler> _masterTaskEventHandlers;
        private readonly Dictionary<string, bool> _repositoriesToRemove;
        private readonly ConcurrentDictionary<string, List<Guid>> _runningTasksPerRepository;

        /// <summary>
        ///     The list of tasks available to spawn slave tasks.
        /// </summary>
        private readonly Dictionary<Guid, IWrapperMasterClass> _tasks;
        private readonly Dictionary<string, List<TaskMessage>> _messagesPerTask;

        private readonly object _tasksLock = new object();
        private readonly List<IAgent> _workingAgents;
        private IScheduler _sched;

        #endregion

        public GridManager(TimeSpan typeCreatorItemLifeTime, TimeSpan itemLifeTimeCheckInterval)
        {
            _messagesPerTask = new Dictionary<string, List<TaskMessage>>();
            _tasks = new Dictionary<Guid, IWrapperMasterClass>();
            _masterTaskEventHandlers = new Dictionary<Guid, MasterTaskEventHandler>();

            _workingAgents = new List<IAgent>();
            _loadedGridTaskElements = new ConcurrentDictionary<string, GridTask>();
            _instanceCreatorsLives = new ConcurrentDictionary<string, InstanceLive>();
            _instanceCreatorsPerRepository = new ConcurrentDictionary<string, ICreateMasterInstance>();
            _runningTasksPerRepository = new ConcurrentDictionary<string, List<Guid>>();
            _repositoriesToRemove = new Dictionary<string, bool>();

            _expiringCreatorsPerRepository = new ExpiringDictionary<string, ICreateMasterInstance>(typeCreatorItemLifeTime, itemLifeTimeCheckInterval);
            _expiringCreatorsPerRepository.KeyRemoved += ExpiringCreatorsPerRepositoryOnKeyRemoved;
            _expiringCreatorsPerRepository.KeyRemoving += ExpiringCreatorsPerRepositoryOnKeyRemoving;

            _agentsPing = new ExpiringDictionary<IAgent, short>(TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(10));
            _agentsPing.KeyRemoved += AgentsPingRemoved;

            _agentsProgress = new ExpiringDictionary<IAgent, TaskProgress>(TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(10));
            _jobDistributor = new FifoJobDistribution(_tasks, _tasksLock, _agentsPing, _workingAgents);

            ISchedulerFactory sf = new StdSchedulerFactory();
            _sched = sf.GetScheduler();

            _sched.Start();
        }

        public static bool EnableTrace { get; set; }
        public static bool EnableMasterCreatorsPing { get; set; }
        public static bool CheckCancelAbuse { get; set; }
        public static bool UseIpcChannel { get; set; }
        public string RepositoryPath { get; set; }

        #region Public methods

        /// <summary>
        ///     Updates the task progress in the agent list.
        ///     This is then used when building the <see cref="GridSummary" />.
        /// </summary>
        /// <param name="agent">The agent.</param>
        /// <param name="taskProgress">The task progress.</param>
        public void UpdateProgress(IAgent agent, TaskProgress taskProgress)
        {
            if (string.IsNullOrEmpty(taskProgress.TaskName))
                throw new ArgumentException("taskProgress.TaskName cannot be null or empty.", "taskProgress");

            _agentsProgress[agent] = taskProgress;
        }

        public void Ping(IAgent agent)
        {
            lock (_agentsPing.SyncLock)
            {
                if (!_agentsPing.ContainsKey(agent))
                    _log.Info("Agent " + agent.MachineName + " connected");

                _agentsPing[agent] = 1;
            }
        }

        /// <summary>
        ///     One thing to clarify between Type.IsSubTypeOf() and Type.IsAssignableFrom():
        ///     IsSubType() will return true only if the given type is derived from the specified type. It will return false if the
        ///     given type IS the specified type.
        ///     IsAssignableFrom() will return true if the given type is either the specified type or derived from the specified
        ///     type.
        ///     So if you are using these to compare BaseClass and DerivedClass (which inherits from BaseClass) then:
        ///     BaseClassInstance.GetType.IsSubTypeOf(GetType(BaseClass)) = FALSE
        ///     BaseClassInstance.GetType.IsAssignableFrom(GetType(BaseClass)) = TRUE
        ///     DerivedClassInstance.GetType.IsSubTypeOf(GetType(BaseClass)) = TRUE
        ///     DerivedClassInstance.GetType.IsAssignableFrom(GetType(BaseClass)) = TRUE
        /// </summary>
        /// <param name="name"></param>
        /// <param name="dirPath"></param>
        /// <returns></returns>
        public TaskSummary AddTaskLibs(string name, string dirPath)
        {
            string slavePath = Path.Combine(dirPath, name, "Slave");
            string masterPath = Path.Combine(dirPath, name, "Master");

            var appDomainSlave = new TasksDiscovery<SlaveTask>("Slave", slavePath);
            var appDomainMaster = new TasksDiscovery<MasterTask>("Master", masterPath);

            var summary = new TaskSummary
            {
                Name = name,
                Progress = new TaskProgress
                {
                    TaskName = name,
                    TaskId = Guid.NewGuid(),
                }
            };

            LoadTaskElements(name, masterPath, appDomainMaster, TaskType.Master);
            LoadTaskElements(name, slavePath, appDomainSlave, TaskType.Slave);

            appDomainMaster.Close();
            appDomainSlave.Close();

            return summary;
        }

        public TaskSummary LaunchTask(string masterId, string slaveId, string customData)
        {
            TaskSummary summary;

            GetMasterTask(masterId, slaveId, customData, out summary);

            return summary;
        }

        // TODO Should add the RepositoryName as parameter since the masterId and slaveId pair could not be unique
        public string ExecuteTask(string masterId, string slaveId, string customData)
        {
            string repositoryName;
            MasterTaskEventHandler masterTaskEventHandler;
            lock (_tasksLock)
            {
                TaskSummary summary;
                var masterTask = GetMasterTask(masterId, slaveId, customData, out summary);
                masterTaskEventHandler = _masterTaskEventHandlers[masterTask.Id];

                repositoryName = masterTask.TaskElement.Name;
                _jobDistributor.AddNewStatistics(repositoryName, masterId);
            }

            var waitEvent = new ManualResetEventSlim(false);

            string result = null;

            var sw = new Stopwatch();
            sw.Start();

            string dateTitle = DateTime.Now.ToLongTimeString();

            masterTaskEventHandler.OnComplete += (o, exception, res) =>
            {
                sw.Stop();

                string taskMessage = string.Format("Task {0} was created at {1}. It took {2} before completion.", masterId, dateTitle, sw.Elapsed.ToString(@"mm\:ss\.ff"));

                if (EnableTrace)
                    _log.Info(taskMessage);

                result = !string.IsNullOrWhiteSpace(exception) ? exception : res;

                lock (_messagesPerTask)
                {
                    List<TaskMessage> messages = !_messagesPerTask.ContainsKey(repositoryName) ? new List<TaskMessage>() : _messagesPerTask[repositoryName];
                    _messagesPerTask[repositoryName] = messages;
                    messages.Add(
                        new TaskMessage
                        {
                            Message =
                                !string.IsNullOrWhiteSpace(exception)
                                    ? string.Format("Task {0} was created at {1}. If failed with error : {2}", masterId, dateTitle, exception)
                                    : taskMessage
                        });
                }

                waitEvent.Set();
            };

            // TODO Add waiting timeout
            waitEvent.Wait();

            return result;
        }

        public void RemoveTaskRepository(string name, string dirPath)
        {
            lock (_lockRemoval)
            {
                PrepareRepositoryRemoval(name);

                //TODO Close any instance creators that have loaded this repository

                try
                {
                    Directory.Delete(Path.Combine(dirPath, name), true);
                }
// ReSharper disable once EmptyGeneralCatchClause
                catch
                {
                }

                var res = _loadedGridTaskElements.Where(e => e.Value.TaskRepositoryName == name);
                foreach (var pair in res)
                {
                    GridTask gridTask;
                    _loadedGridTaskElements.TryRemove(pair.Key, out gridTask);
                }

                List<Guid> runningTasks;
                if (_runningTasksPerRepository.TryRemove(name, out runningTasks))
                {
                    runningTasks.Clear();
                }

                _repositoriesToRemove.Remove(name);
            }
        }

        public List<GridTask> GetGridTasks(string name)
        {
            var res = _loadedGridTaskElements.Values.ToList();
            if (!string.IsNullOrWhiteSpace(name))
            {
                res = res.Where(e => e.TaskRepositoryName == name).ToList();
            }

            return res;
        }

        /// <summary>
        ///     Gets the grid summary, representing the current
        ///     state of the Grid. <seealso cref="GridSummary" />
        /// </summary>
        /// <returns>A grid summary.</returns>
        public GridSummary GetGridSummary()
        {
            var summary = new GridSummary
            {
                DistributionStats = _jobDistributor.GetGridStatistics(),
                Statistics = _jobDistributor.GetTasksExecutionStatistics(),
            };

            lock (_messagesPerTask)
            {
                _messagesPerTask.ToList().ForEach(e => summary.MessagesPerTask.Add(e.Key, e.Value));
            }

            lock (_agentsPing.SyncLock)
            {
                _agentsPing.ToList().ForEach(e => summary.ConnectedAgents.Add((Agent) e.Key));
            }
            lock (_workingAgents)
            {
                summary.WorkingAgents.AddRange(_workingAgents.Select(a => (Agent) a));
            }

            lock (_tasksLock)
            {
                List<KeyValuePair<Guid, IWrapperMasterClass>> tasks = _tasks.ToList();

                foreach (var pair in tasks)
                {
                    try
                    {
                        var taskElement = pair.Value.TaskElement;
                        if (taskElement != null)
                        {
                            var task = pair.Value;
                            var taskSummary = new TaskSummary
                            {
                                Name = task.Name,
                                Progress = {StepsCompleted = task.StepsCompleted, StepsGoal = task.StepsGoal, TaskId = task.Id, TaskName = taskElement.MasterId}
                            };
                            summary.TaskSummaries.Add(taskSummary);
                        }
                    }
                    catch (SocketException ex)
                    {
                        pair.Value.FireFailedCompletion(new TaskException("GetGridSummary -> Socket Exception", ex).JsonException);
                    }
                    catch (Exception ex)
                    {
                        _log.Info("GetGridSummary Tasks: Exception other then SocketException" + JsonConvert.SerializeObject(ex, Formatting.Indented));
                        pair.Value.FireFailedCompletion(new TaskException("GetGridSummary", ex).JsonException);
                    }
                }

                /* This is not particularly efficient. 
                 * We can make improvements here. */
                foreach (KeyValuePair<IAgent, TaskProgress> pair in _agentsProgress)
                {
                    try
                    {
                        IWrapperMasterClass task;
                        if (_tasks.TryGetValue(pair.Value.TaskId, out task))
                        {
                            var taskSummary = summary.TaskSummaries.First(fo => fo.Progress.TaskId == task.Id);
                            //if (taskSummary == null)
                            //{
                            //    taskSummary = new TaskSummary {Name = task.Name};
                            //    summary.TaskSummaries.Add(taskSummary);
                            //}
                            taskSummary.AgentCount++;
                            taskSummary.MFlops += pair.Key.MFlops;
                            taskSummary.BandwidthKBps += pair.Key.BandwidthKBps;
                        }
                    }
                    catch (SocketException)
                    {
                    }
                    catch (Exception ex)
                    {
                        _log.Info("GetGridSummary Agents : Exception other then SocketException" + JsonConvert.SerializeObject(ex, Formatting.Indented));
                    }
                }
            }

            return summary;
        }

        /* Used for deciding the next task to be served. */

        public TaskDescriptor GetDescriptor(IAgent agent)
        {
            if (!agent.MachineName.ToLower().Contains("rec01"))
                return new TaskDescriptor { Enabled = false, Job = new Job(0) };

            IMasterTask masterTask = null;
            try
            {
                //TODO Use stats when distributing jobs
                var res = _jobDistributor.GetDescriptor(agent, out masterTask);

                if (masterTask != null && res.Enabled)
                {
                    var taskElement = masterTask.TaskElement;
                    _jobDistributor.SetDivisonStats(agent, taskElement, true);
                    lock (_workingAgents)
                    {
                        _workingAgents.Add(agent);
                    }
                }

                return res;
            }
            catch (SerializationException ex)
            {
                return TreatTaskDescriptorLoadingError(masterTask, ex);
            }
            catch (Exception)
            {
                return new TaskDescriptor {Enabled = false, Job = new Job(0)};
            }
        }

        /// <summary>
        ///     Cancels the task
        /// </summary>
        /// <param name="agent">The agent.</param>
        /// <param name="info">The task information.</param>
        public void Cancel(IAgent agent, TaskInformation info)
        {
            if (info == null)
                throw new ArgumentNullException("info");

            if (string.IsNullOrEmpty(info.TaskName))
                throw new ArgumentException("taskResult.TaskName should not be null.", "info");

            lock (_workingAgents)
            {
                _workingAgents.Remove(agent);
            }

            GridTaskElement taskElement;
            // TODO Check if implementation is complete
            lock (_tasksLock)
            {
                IWrapperMasterClass task;
                if (!_tasks.TryGetValue(info.TaskId, out task))
                {
                    var errorMessage = string.Format("Agent MachineName : \"{0}\" Username : \"{1}\" cancels operation, but the task specified as taskResult.TaskName \"{2}\" does not exist.",
                        agent.IPAddress,
                        agent.UserName,
                        info.TaskName);

                    if (EnableTrace)
                        _log.WarnFormat(errorMessage);

                    throw new ArgumentException(errorMessage);
                }

                task.Cancel(agent, info.JobId);
                taskElement = task.TaskElement;
            }

            _jobDistributor.OnCheckCancelAbuse(agent, info);
            if (taskElement != null)
            {
                _jobDistributor.SetDivisonStats(agent, taskElement, false, cancel: true);
            }
        }

        public void Abort(string masterId)
        {
            lock (_tasksLock)
            {
                // TODO Keep a list of running master tasks ids per masterTask type
                var masterTaskId = new Guid(masterId);

                IWrapperMasterClass masterClass;
                if (_tasks.TryGetValue(masterTaskId, out masterClass))
                {
                    masterClass.FireFailedCompletion(new TaskException("Task is aborted by user").JsonException);
                }
            }
        }

        /// <summary>
        ///     Joins the specified agent's task result
        ///     to the associated <see cref="MasterTask" />.
        /// </summary>
        /// <param name="agent">The agent.</param>
        /// <param name="result">The task result.</param>
        public void Join(IAgent agent, TaskResult result)
        {
            if (result == null)
                throw new ArgumentNullException("result");

            if (string.IsNullOrEmpty(result.TaskName))
                throw new ArgumentException("taskResult.TaskName should not be null.", "result");

            lock (_workingAgents)
            {
                _workingAgents.Remove(agent);
            }

            GridTaskElement taskElement = null;
            lock (_tasksLock)
            {
                IWrapperMasterClass task;
                if (!_tasks.TryGetValue(result.TaskId, out task))
                {
                    var errorMessage = string.Format("Agent MachineName : \"{0}\" Username : \"{1}\" joined operation, but task specified as taskResult.TaskName \"{2}\" does not exist.",
                        agent.IPAddress,
                        agent.UserName,
                        result.TaskName);

                    if (EnableTrace)
                        _log.WarnFormat(errorMessage);

                    throw new ArgumentException(errorMessage);
                }
                try
                {
                    task.Join(agent, result);
                    taskElement = task.TaskElement;
                }
                catch (SocketException ex)
                {
                    task.FireFailedCompletion(new TaskException("GridManager Join", ex).JsonException);

                    if (EnableTrace)
                        _log.Warn("Grid Manager Join -> Socket Error: " + JsonConvert.SerializeObject(ex));
                }
                catch (Exception ex)
                {
                    if (EnableTrace)
                        _log.Warn("Grid Manager Join -> Error: " + JsonConvert.SerializeObject(ex));
                }
            }

            if (taskElement != null)
            {
                _jobDistributor.SetDivisonStats(agent, taskElement, init: false);
            }
        }

        #endregion

        #region Private methods

        private TaskDescriptor TreatTaskDescriptorLoadingError(IMasterTask masterTask, SerializationException ex)
        {
            if (masterTask != null)
            {
                // If we have an task build exception set an error on this task
                // so the agent wont try to load it again and again
                lock (_tasksLock)
                {
                    IWrapperMasterClass task;
                    if (_tasks.TryGetValue(masterTask.Id, out task))
                    {
                        GridTaskElement taskElement = task.TaskElement;
                        string name = taskElement.Name;

                        var res = _loadedGridTaskElements.Where(e => e.Value.TaskRepositoryName == name);
                        foreach (var pair in res)
                        {
                            if (pair.Value.Id == taskElement.MasterId)
                                pair.Value.State = GridTaskState.CannotBeLoaded;
                        }

                        task.FireFailedCompletion(new TaskException("TreatTaskDescriptorLoadingError", ex).JsonException);
                    }
                }
            }
            return new TaskDescriptor {Enabled = false, Job = new Job(0)};
        }

        private void PrepareRepositoryRemoval(string name)
        {
            if (_expiringCreatorsPerRepository.ContainsKey(name))
            {
                // Mark this repository to be removed. This blocks any further execution of its master tasks
                // and waits for all its running master tasks to be finished. Then after it will be removed 
                // by the expiration process
                if (!_repositoriesToRemove.ContainsKey(name))
                    _repositoriesToRemove.Add(name, true);

                List<Guid> repositoryRunningTasks;
                if (_runningTasksPerRepository.TryGetValue(name, out repositoryRunningTasks))
                {
                    var repositoryTasks = _loadedGridTaskElements.Where(e => e.Value.TaskRepositoryName == name).Select(e => e.Value).ToList();

                    lock (_tasksLock)
                    {
                        var runningGridTasks = _tasks.Where(e => repositoryRunningTasks.Contains(e.Value.Id)).ToList();

                        foreach (var task in repositoryTasks)
                        {
                            task.State = runningGridTasks.Any(e => e.Value.TaskElement.MasterId == task.Id) ? GridTaskState.RunningBeforeRemoval : GridTaskState.WaitingForRemoval;
                        }
                    }
                }

                throw new Exception("Repository " + name + " is still in its live period.");
            }
        }

        private IMasterTask GetMasterTask(string masterId, string slaveId, string customData, out TaskSummary summary)
        {
            IMasterTask masterTask;

            // Lock order (_tasksLock => _lockRunningTasksPerRepository) is compulsory
            lock (_tasksLock)
            {
                lock (_lockRunningTasksPerRepository)
                {
                    var taskElement = PrepareTaskExecution(masterId, slaveId, customData, out summary);

                    string taskRepositoryName = taskElement.Name;

                    _ignoreExpirationRepositories.Add(taskRepositoryName);

                    try
                    {
                        masterTask = new WrapperMasterClass(taskElement.Build());

                        masterTask.SetTraceValue(EnableTrace);
                        var masterTaskEventHandler = new MasterTaskEventHandler(masterId, masterTask);
                        masterTaskEventHandler.OnComplete += TaskComplete;

                        Guid masterTaskId = masterTask.Id;

                        _masterTaskEventHandlers.Add(masterTaskId, masterTaskEventHandler);
                        _tasks.Add(masterTaskId, (IWrapperMasterClass) masterTask);

                        List<Guid> runningsTasks;
                        if (!_runningTasksPerRepository.TryGetValue(taskRepositoryName, out runningsTasks))
                        {
                            runningsTasks = new List<Guid>();
                            _runningTasksPerRepository.TryAdd(taskRepositoryName, runningsTasks);
                        }

                        runningsTasks.Add(masterTaskId);
                    }
                    finally
                    {
                        _ignoreExpirationRepositories.Remove(taskRepositoryName);
                    }
                }
            }
            return masterTask;
        }

        private void TaskComplete(TaskInfo info, string e, string res)
        {
            Guid taskId = info.MasterTaskId;
            GridTaskElement taskElement = info.TaskElement;
            string repositoryId = taskElement.MasterId;
            string repositoryName = taskElement.Name;

            if (EnableTrace)
            {
                if (!string.IsNullOrWhiteSpace(e))
                {
                    _log.Error(string.Format("Master task {0} has failed", repositoryName), new Exception(e));
                }
                else
                    _log.Info(string.Format("Master task {0} is completed", repositoryName));
            }

            lock (_lockRunningTasksPerRepository)
            {
                var gridTask = _loadedGridTaskElements.First(g => g.Value.Id == repositoryId);
                if (gridTask.Value.State != GridTaskState.RunningBeforeRemoval && gridTask.Value.State != GridTaskState.CannotBeLoaded)
                    gridTask.Value.State = GridTaskState.WaitingForExecution;

                // There is a lock (_tasksLock) around the Task.Join method (or FireFailedCompletion....or GetJob method on the masterTask class), so there's no need to lock it again...
                // Remove master task on completion
                _tasks.Remove(taskId);
                _masterTaskEventHandlers.Remove(taskId);
                List<Guid> runningsTasks;
                if (_runningTasksPerRepository.TryGetValue(repositoryName, out runningsTasks))
                {
                    // Remove it also from its repository running tasks
                    runningsTasks.Remove(taskId);

                    // If there's no any running tasks for this repository, removed the key from
                    // the dictionary
                    if (!runningsTasks.Any())
                    {
                        if (_runningTasksPerRepository.TryRemove(repositoryName, out runningsTasks))
                        {
                            // If there's an error close the instance creator of this repository
                            if (!string.IsNullOrWhiteSpace(e))
                            {
                                if (EnableTrace)
                                    _log.Info("Master task execution has error so we close the process that hosts it");

                                RemoveInstanceCreatorOfRepository(repositoryName);
                            }
                        }
                    }
                }
            }

            _log.Info("Master task '" + repositoryName + "' completed and there are still " + _tasks.Count + " tasks!");
        }

        private void ExpiringCreatorsPerRepositoryOnKeyRemoving(string key, ref bool cancel)
        {
            lock (_lockRunningTasksPerRepository)
            {
                if (_ignoreExpirationRepositories.Contains(key))
                {
                    cancel = true;
                    return;
                }

                List<Guid> runningsTasks;
                if (_runningTasksPerRepository.TryGetValue(key, out runningsTasks))
                {
                    // if the repository contains tasks in execution dont close it
                    cancel = runningsTasks.Any();
                }
            }
        }

        private void ExpiringCreatorsPerRepositoryOnKeyRemoved(string key)
        {
            RemoveInstanceCreatorOfRepository(key);
        }

        private void RemoveInstanceCreatorOfRepository(string repositoryName)
        {
            lock (_lockRunningTasksPerRepository)
            {
                ICreateMasterInstance instance;
                if (_instanceCreatorsPerRepository.TryRemove(repositoryName, out instance))
                {
                    if (instance is RemoteServerConnector)
                    {
                        (instance as RemoteServerConnector).RemoteServerClosed -= InstanceRemoteServerClosed;
                    }
                    InstanceLive instanceLive;
                    _instanceCreatorsLives.TryRemove(repositoryName, out instanceLive);
                    _expiringCreatorsPerRepository.Remove(repositoryName);

                    try
                    {
                        instance.Close();
                    }
// ReSharper disable once EmptyGeneralCatchClause
                    catch (Exception)
                    {
                    }

                    lock (_lockRemoval)
                    {
                        if (_repositoriesToRemove.ContainsKey(repositoryName))
                        {
                            RemoveTaskRepository(repositoryName, RepositoryPath);

                            _repositoriesToRemove.Remove(repositoryName);
                        }
                    }
                }
            }
        }

        private void AgentsPingRemoved(IAgent agent)
        {
            var tasks = new List<IMasterTask>();
            lock (_tasksLock)
            {
                tasks.AddRange(_tasks.Select(pair => pair.Value));
            }

            if (EnableTrace)
                _log.InfoFormat("The agent {0} got lost!", agent.MachineName);

            foreach (var masterTask in tasks)
            {
                masterTask.LostAgent(agent);
            }
        }

        private void LoadTaskElements<T>(string name, string path, TasksDiscovery<T> appDomain, TaskType taskType)
        {
            string masterTaskAssemblyName = typeof (MasterTask).Assembly.GetName().Name;
            string slaveTaskAssemblyName = typeof (SlaveTask).Assembly.GetName().Name;

            bool containsUnmanagedDll = false;
            bool containsx32BitsDll = false;
            bool containsx64BitsDll = false;

            var tasks = new Dictionary<string, GridTask>();
            var manager = new AssemblyReflectionManager();

            foreach (string dllFile in Directory.EnumerateFiles(path, "*.dll"))
            {
                if (new FileInfo(dllFile).Name.Contains(masterTaskAssemblyName))
                    continue;

                if (new FileInfo(dllFile).Name.Contains(slaveTaskAssemblyName))
                    continue;

                #region Get platform target

                string platformTarget = "";
                const string domainName = "demodomain";
                if (manager.LoadAssembly(dllFile, domainName))
                {
                    PortableExecutableKinds peKind;
                    ImageFileMachine imageFileMachine = manager.GetDllMachineType(dllFile, out peKind);

                    if (peKind == PortableExecutableKinds.Required32Bit)
                        containsx32BitsDll = true;

                    if (peKind == PortableExecutableKinds.Unmanaged32Bit)
                    {
                        containsx32BitsDll = true;
                        containsUnmanagedDll = true;
                    }

                    if (peKind == PortableExecutableKinds.PE32Plus)
                    {
                        containsx64BitsDll = true;
                    }

                    platformTarget = string.Format("{0} {1}", Enum.GetName(typeof (ImageFileMachine), imageFileMachine), Enum.GetName(typeof (PortableExecutableKinds), peKind));

                    manager.UnloadAssembly(dllFile);
                }
                manager.UnloadDomain(domainName);

                #endregion

                try
                {
                    var gridTasks = appDomain.GetGridTasks(dllFile);

                    foreach (var task in gridTasks)
                    {
                        var gridTask = new GridTask
                        {
                            Name = task.Name,
                            Id = task.Id ?? Guid.NewGuid().ToString(),
                            Type = task.ImplementationType == ImplementationType.Free ? taskType : TaskType.MasterLight,
                            TaskRepositoryName = name,
                            ImplementationType = task.ImplementationType,
                            DllLocation = dllFile,
                            CreatorType = InstanceCreatorType.NewAppDomainProxy,
                            AssemblyName = Path.GetFileNameWithoutExtension(new DirectoryInfo(dllFile).Name),
                            PlatformTarget = platformTarget,
                            State = GridTaskState.WaitingForExecution,
                        };

                        tasks.Add(task.FullName, gridTask);
                    }
                }
                catch (Exception ex)
                {
                    if (!containsx32BitsDll && !containsx64BitsDll && !containsUnmanagedDll)
                    {
                        string message = string.Format("LoadTaskElements for task '{0}' at '{1}' failed", name, path);
                        _log.Error(message, ex);
                    }
                }
            }

            foreach (var pair in tasks)
            {
                if (containsx32BitsDll || containsx64BitsDll || containsUnmanagedDll)
                {
                    pair.Value.CreatorType = InstanceCreatorType.RemoteProxy;
                }

                // There are dlls which can load native dlls so they should be executed into a remoteproxy.
                // TODO -> Better algo for Execution type detection. For the moment we force the exection into a remote proxy
                pair.Value.CreatorType = InstanceCreatorType.RemoteProxy;

                _loadedGridTaskElements.TryAdd(pair.Key, pair.Value);
            }
        }

        private TaskElement PrepareTaskExecution(string masterId, string slaveId, string customData, out TaskSummary summary)
        {
            var elemMaster = _loadedGridTaskElements.FirstOrDefault(e => e.Value.Id == masterId);
            var elemSlave = _loadedGridTaskElements.FirstOrDefault(e => e.Value.Id == slaveId);

            if (elemMaster.Value == null)
                throw new Exception("Couldnt find the master task with id : " + masterId);

            if (elemSlave.Value == null)
                throw new Exception("Couldnt find the slave task with id : " + slaveId);

            if (elemSlave.Value.Type != TaskType.Slave)
                throw new Exception("The slave id doesn't correspond to a slave task");

            if (elemMaster.Value.Type != TaskType.Master && elemMaster.Value.Type != TaskType.MasterLight)
                throw new Exception("The master id doesn't correspond to a master task");

            string name = elemMaster.Value.Name;

            Guid taskElementId = Guid.NewGuid();

            summary = new TaskSummary
            {
                Name = name,
                Progress = new TaskProgress
                {
                    TaskName = name,
                    TaskId = taskElementId,
                },
            };

            GridTask gridMasterTask = elemMaster.Value;
            if (gridMasterTask.State == GridTaskState.CannotBeLoaded)
                throw new TypeLoadException("Cannot load the master task");

            if (_repositoriesToRemove.ContainsKey(gridMasterTask.TaskRepositoryName))
                throw new Exception("Repository will be removed soon : " + gridMasterTask.TaskRepositoryName);

            gridMasterTask.State = GridTaskState.Running;

            var masterInstanceCreator = GetMasterInstanceCreator(gridMasterTask);

            var taskElement = new TaskElement
            {
                Name = gridMasterTask.TaskRepositoryName,
                Id = taskElementId.ToString(),
                MasterId = masterId,
                ImplementationType = gridMasterTask.ImplementationType,
                DllLocation = gridMasterTask.DllLocation,
                TypeName = elemMaster.Key,
                SlaveTypeName = elemSlave.Key,
                SlaveTypeAssemblyName = elemSlave.Value.AssemblyName,
                CreateInstance = masterInstanceCreator,
                CustomProviderData = customData,
                CreatorType = gridMasterTask.CreatorType,
            };

            return taskElement;
        }

        private ICreateMasterInstance GetMasterInstanceCreator(GridTask gridMasterTask)
        {
            string taskRepositoryName = gridMasterTask.TaskRepositoryName;

            ICreateMasterInstance masterInstanceCreator;
            //lock (_expiringCreatorsPerRepository.SyncLock)
            {
                if (!_expiringCreatorsPerRepository.TryGetValue(taskRepositoryName, out masterInstanceCreator))
                {
                    masterInstanceCreator = gridMasterTask.CreatorType == InstanceCreatorType.NewAppDomainProxy
                        ? new MasterAppDomainCreateInstantiator<MasterTask>("appDomainMaster-" + taskRepositoryName) :
                        (ICreateMasterInstance)new RemoteServerConnector(taskRepositoryName, useIpcChannel: UseIpcChannel, isClient: false, enableMasterCreatorsPing: EnableMasterCreatorsPing);

                    var instance = masterInstanceCreator as RemoteServerConnector;
                    if (instance != null)
                    {
                        instance.RemoteServerClosed += InstanceRemoteServerClosed;
                    }

                    _expiringCreatorsPerRepository.Add(taskRepositoryName, masterInstanceCreator);
                    _instanceCreatorsPerRepository.TryAdd(taskRepositoryName, masterInstanceCreator);
                    _instanceCreatorsLives.TryAdd(taskRepositoryName, new InstanceLive {LivesCount = 1, LastAccess = DateTime.UtcNow, Instance = masterInstanceCreator});
                }
                else
                {
                    _expiringCreatorsPerRepository.Touch(taskRepositoryName);

                    var liveInstance = _instanceCreatorsLives[taskRepositoryName];
                    liveInstance.LivesCount++;
                    liveInstance.LastAccess = DateTime.UtcNow;
                }
            }
            return masterInstanceCreator;
        }

        private void InstanceRemoteServerClosed(ICreateMasterInstance instanceCreator)
        {
            if (EnableTrace)
                _log.Info("Instance of RemoteServer closed");

            // Lock order (_tasksLock => _lockRunningTasksPerRepository) is compulsory
            lock (_tasksLock)
            {
                lock (_lockRunningTasksPerRepository)
                {
                    var pair = _instanceCreatorsPerRepository.FirstOrDefault(e => e.Value == instanceCreator);
                    if (pair.Key != null)
                    {
                        RemoveInstanceCreatorOfRepository(pair.Key);

                        List<Guid> runningTasks;
                        if (_runningTasksPerRepository.TryRemove(pair.Key, out runningTasks))
                        {
                            runningTasks.ForEach(g =>
                            {
                                IWrapperMasterClass masterClass;
                                if (_tasks.TryGetValue(g, out masterClass))
                                {
                                    masterClass.FireFailedCompletion(new TaskException("Hosting instance creator closed").JsonException);
                                }
                            });
                        }
                    }
                }
            }
        }


        #endregion

        private class InstanceLive
        {
            public ICreateMasterInstance Instance { get; set; }
            public int LivesCount { get; set; }
            public DateTime LastAccess { get; set; }
        }

        public void ScheduleTask(string repositoryName, string masterId, string slaveId, string cronExpression, string customData)
        {
            string jobName = "job" + masterId + slaveId;

            IJobDetail job = JobBuilder.Create<TaskJob>()
                .WithIdentity(jobName, repositoryName)
                .Build();

            var jobInfo = new TaskJobInfo
            {
                RepositoryName = repositoryName,
                MasterId = masterId,
                SlaveId = slaveId,
                CronExpression = cronExpression,
                CustomData = customData,
                GridInstance = this,
            };

            job.JobDataMap["jobInfo"] = jobInfo;

            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity(jobName, repositoryName)
                .WithCronSchedule(cronExpression, builder => builder.WithMisfireHandlingInstructionIgnoreMisfires())
                .Build();

            _sched.ScheduleJob(job, trigger);
            var jobKeys = _sched.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
            foreach (var jobKey in jobKeys)
            {
                var jobDetail = _sched.GetJobDetail(jobKey);
                var jobData = (TaskJobInfo)jobDetail.JobDataMap.Get("jobInfo");

                Console.WriteLine(jobData.MasterId);
            }
            Console.WriteLine("test");
        }
    }
}