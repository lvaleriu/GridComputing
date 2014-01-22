#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Remoting;
using System.Threading;
using GridAgentSharedLib;
using GridAgentSharedLib.Clients;
using GridComputing.Collections;
using GridComputingSharedLib;
using GridSharedLibs;
using Newtonsoft.Json;

#endregion

namespace GridComputing.JobDistributions
{
    public abstract class JobDistributionBase : IJobDistribution, IDisposable
    {
        #region Fields

        private const int MaxCancellationCount = 3;
        private static readonly IGridLog Log = GridLogManager.GetLogger(typeof (JobDistributionBase));
        private readonly ExpiringDictionary<IAgent, short> _agentsPing;
        private readonly Dictionary<IAgent, IMasterTask> _assignedMasterTasksPerAgent;
        private readonly Dictionary<IAgent, TaskDescriptor> _assignedTaskDescriptorsPerAgent;
        private readonly ExpiringDictionary<Tuple<Guid, string>, bool> _avoidTasksPerUser;
        private readonly Dictionary<Tuple<Guid, string>, int> _cancelledTasksPerUser;

        private readonly Dictionary<string, Tuple<List<string>, double>> _computingDistribution;
        private readonly object _lockGetJob = new object();
        private readonly Dictionary<Tuple<string, string>, List<DateTime>> _repositoryTasksDistribution;

        private readonly Dictionary<Guid, IWrapperMasterClass> _tasks;
        private readonly Dictionary<Tuple<string, string>, TaskExecutionStatistics> _tasksExecutionStatistics;
        private readonly object _tasksLock;
        private readonly Timer _timer;
        private readonly Dictionary<IAgent, ManualResetEventSlim> _waitingAgents;
        private readonly List<IAgent> _workingAgents;
        private volatile bool _gatheringCondition = true;
        private volatile int _gatheringCount;

        #endregion

        protected JobDistributionBase(Dictionary<Guid, IWrapperMasterClass> tasks,
            object tasksLock,
            ExpiringDictionary<IAgent, short> agentsPing,
            List<IAgent> workingAgents)
        {
            _tasks = tasks;
            _tasksLock = tasksLock;

            _agentsPing = agentsPing;

            _workingAgents = workingAgents;
            _tasksExecutionStatistics = new Dictionary<Tuple<string, string>, TaskExecutionStatistics>();

            _cancelledTasksPerUser = new Dictionary<Tuple<Guid, string>, int>();
            _avoidTasksPerUser = new ExpiringDictionary<Tuple<Guid, string>, bool>(TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(10));
            _avoidTasksPerUser.KeyRemoved += AvoidTasksPerUserOnKeyRemoved;

            _computingDistribution = new Dictionary<string, Tuple<List<string>, double>>();
            _waitingAgents = new Dictionary<IAgent, ManualResetEventSlim>();


            _assignedTaskDescriptorsPerAgent = new Dictionary<IAgent, TaskDescriptor>();
            _assignedMasterTasksPerAgent = new Dictionary<IAgent, IMasterTask>();
            _repositoryTasksDistribution = new Dictionary<Tuple<string, string>, List<DateTime>>();

            _timer = new Timer(SetJobsForWaitingAgents, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(300));
        }

        public void Dispose()
        {
            if (_timer != null)
            {
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
                _timer.Dispose();
            }
        }

        public Dictionary<string, List<LightTaskExecutionStatistics>> GetTasksExecutionStatistics()
        {
            var res = new Dictionary<string, List<LightTaskExecutionStatistics>>();

            lock (_tasksExecutionStatistics)
            {
                foreach (KeyValuePair<Tuple<string, string>, TaskExecutionStatistics> pair in _tasksExecutionStatistics)
                {
                    if (!res.ContainsKey(pair.Value.RepositoryName))
                    {
                        res.Add(pair.Value.RepositoryName, new List<LightTaskExecutionStatistics>());
                    }
                    var list = res[pair.Value.RepositoryName];

                    var statistics = new LightTaskExecutionStatistics
                    {
                        MasterId = pair.Value.MasterId,
                        StatsPerAgent = pair.Value.StatsPerAgent.Values.Select(e => JsonConvert.DeserializeObject<LightDivisionExecutionStat>(JsonConvert.SerializeObject(e))).ToList()
                    };

                    statistics.StatsPerAgent.ForEach(s =>
                    {
                        if (s.Durations.Any())
                        {
                            s.AverageDuration = TimeSpan.FromSeconds(s.Durations.Average(e => e.TotalSeconds));
                            s.Durations.Reverse();
                            s.Durations = s.Durations.Take(10).ToList();
                            s.Durations.Reverse();
                        }
                    });

                    list.Add(statistics);
                }
            }

            return res;
        }

        public void SetDivisonStats(IAgent agent, GridTaskElement taskElement, bool init, bool cancel = false)
        {
            var key = new Tuple<string, string>(taskElement.Name, taskElement.MasterId);

            lock (_tasksExecutionStatistics)
            {
                var statistics = _tasksExecutionStatistics[key];
                if (!statistics.StatsPerAgent.ContainsKey(agent.Id))
                {
                    statistics.StatsPerAgent.Add(agent.Id, new DivisionExecutionStat
                    {
                        AgentInfo = (Agent) agent,
                    });
                }

                var agentStat = statistics.StatsPerAgent[agent.Id];
                agentStat.ExecutionCount += init ? 0 : 1;
                var now = DateTime.UtcNow;
                if (!init)
                {
                    var duration = now - agentStat.LastDistributionTImeUtc;
                    agentStat.Durations.Add(duration);
                    agentStat.Dates.Add(new ExecutionTime {Date = agentStat.LastDistributionTImeUtc, Duration = duration});
                }
                agentStat.LastDistributionTImeUtc = now;

                if (cancel)
                {
                    statistics.StatsPerAgent.Remove(agent.Id);
                }
            }
        }

        public void AddNewStatistics(string repositoryName, string masterId)
        {
            lock (_tasksExecutionStatistics)
            {
                var key = new Tuple<string, string>(repositoryName, masterId);
                if (!_tasksExecutionStatistics.ContainsKey(key))
                    _tasksExecutionStatistics.Add(key, new TaskExecutionStatistics {MasterId = masterId, RepositoryName = repositoryName});
            }
        }

        public void OnCheckCancelAbuse(IAgent agent, TaskInformation info)
        {
            if (!GridManager.CheckCancelAbuse)
                return;

            lock (_avoidTasksPerUser.SyncLock)
            {
                int cnt = 0;
                var key = new Tuple<Guid, string>(agent.Id, info.TaskName);
                if (_cancelledTasksPerUser.ContainsKey(key))
                {
                    cnt = _cancelledTasksPerUser[key];
                }

                cnt++;

                _cancelledTasksPerUser[key] = cnt;

                if (cnt == MaxCancellationCount)
                {
                    if (GridManager.EnableTrace)
                        Log.Info(string.Format("User {0} abuses of the cancellation for the following task {1}", agent.MachineName, info.TaskName));

                    _avoidTasksPerUser.Add(key, true);
                }
                if (cnt > MaxCancellationCount)
                {
                    _avoidTasksPerUser.Touch(key);
                }
            }
        }

        public GridDistributionStatistics GetGridStatistics()
        {
            var stats = new GridDistributionStatistics();

            int availableAgents;
            lock (_agentsPing.SyncLock)
            {
                availableAgents = _agentsPing.Count;
            }

            lock (_tasksExecutionStatistics)
            {
                stats.Count = _tasksExecutionStatistics.Select(e => e.Value.StatsPerAgent.Select(f => f.Value.ExecutionCount).Sum()).Sum();
                stats.TotalDuration = TimeSpan.FromSeconds(_tasksExecutionStatistics.Select(e => e.Value.StatsPerAgent.Select(f => f.Value.Durations.Sum(t => t.TotalSeconds)).Sum()).Sum());

                foreach (var groupByRep in _tasksExecutionStatistics.Keys.GroupBy(b => b.Item1))
                {
                    var repStat = new RepositoryStat {Name = groupByRep.Key};

                    foreach (var repMaster in groupByRep)
                    {
                        var repExecStats = _tasksExecutionStatistics[repMaster];
                        var masterStat = new MasterStat
                        {
                            CountPer = (double)repExecStats.StatsPerAgent.Sum(f => f.Value.Durations.Count)/stats.Count,
                            TimePerc = repExecStats.StatsPerAgent.Sum(f => f.Value.Durations.Sum(t => t.TotalSeconds)) / stats.TotalDuration.TotalSeconds,
                            AgentsPerc = (double)repExecStats.StatsPerAgent.Count / availableAgents,
                        };

                        repStat.Stats.Add(repMaster.Item2, masterStat);
                    }

                    repStat.CountPer = repStat.Stats.Values.Sum(e => e.CountPer);
                    repStat.TimePerc = repStat.Stats.Values.Sum(e => e.TimePerc);

                    stats.RepositoryStats.Add(repStat);
                }

                var dates = _tasksExecutionStatistics.Values.SelectMany(e => e.StatsPerAgent.Values.SelectMany(f => f.Dates.Select(d => d.Date.Date).Distinct())).Distinct();
                foreach (var date in dates)
                {
                    var dailyStat = new GridDistributionDailyStatistics();
                    stats.DailyStatistics.Add(date, dailyStat);

                    foreach (var groupByRep in _tasksExecutionStatistics.Keys.GroupBy(b => b.Item1))
                    {
                        var repCount = new RepCount {Name = groupByRep.Key};
                        foreach (var repMaster in groupByRep)
                        {
                            var repExecStats = _tasksExecutionStatistics[repMaster];
                            var masterCount = new MasterCount
                            {
                                Count = repExecStats.StatsPerAgent.Sum(e => e.Value.Dates.Count(f => f.Date.Date == date)),
                                Duration = TimeSpan.FromSeconds(repExecStats.StatsPerAgent.Sum(e => e.Value.Dates.Where(f => f.Date.Date == date).Sum(g => g.Duration.TotalSeconds))),
                            };

                            repCount.Tasks.Add(repMaster.Item2, masterCount);
                        }
                        repCount.Count = repCount.Tasks.Sum(e => e.Value.Count);
                        repCount.Duration = TimeSpan.FromSeconds(repCount.Tasks.Sum(e => e.Value.Duration.TotalSeconds));

                        if (repCount.Tasks.Any())
                        {
                            dailyStat.Repositories.Add(repCount);
                        }
                    }

                    dailyStat.Count = dailyStat.Repositories.Sum(e => e.Count);
                    dailyStat.Duration = TimeSpan.FromSeconds(dailyStat.Repositories.Sum(e => e.Duration.TotalSeconds));
                }
            }

            return stats;
        }

        public void SetComputingDistribution(Dictionary<string, Tuple<List<string>, double>> computingDistribution)
        {
            lock (_computingDistribution)
            {
                _computingDistribution.Clear();
                computingDistribution.ToList().ForEach(e => _computingDistribution.Add(e.Key, e.Value));
            }
        }

        public TaskDescriptor GetDescriptor(IAgent agent, out IMasterTask masterTask)
        {
            TaskDescriptor descriptor;

            var waitEvent = AddWaitingAgent(agent);

            waitEvent.Wait();

            RemoveWaitingAgent(agent, out masterTask, out descriptor);

            if (descriptor.Job == null)
            {
                return new TaskDescriptor {Enabled = false, Job = new Job(0)};
            }

            if (descriptor.Job != null && descriptor.Enabled)
            {
                SetDistributionStat(masterTask, descriptor);
            }

            return descriptor;
        }

        public TaskDescriptor OldGetDescriptor(IAgent agent, out IMasterTask masterTask)
        {
            var descriptor = new TaskDescriptor();
            masterTask = null;

            List<string> avoidTaskList;

            lock (_avoidTasksPerUser.SyncLock)
            {
                avoidTaskList = _avoidTasksPerUser.Keys.Where(e => e.Item1 == agent.Id).Select(e => e.Item2).ToList();
            }

            lock (_lockGetJob)
            {
                {
                    // As there are many calls on this method (GetDescriptor) adding one element to the _tasks collection can take some time!
                    Thread.Sleep(TimeSpan.FromMilliseconds(100));

                    lock (_tasksLock)
                    {
                        Guid masterGuid = Guid.Empty;
                        bool foundJob = GetNextMasterTask(ref masterTask, avoidTaskList, ref masterGuid, _tasks);

                        if (masterTask != null && foundJob)
                        {
                            descriptor = SetJobForAgent(agent, masterTask, masterGuid);
                        }
                    }
                }
            }

            if (descriptor.Job == null)
            {
                //if (GridManager.EnableTrace)
                //    Log.Info("Master task returned no job");

                return new TaskDescriptor {Enabled = false, Job = new Job(0)};
            }

            return descriptor;
        }

        protected abstract bool GetNextMasterTask(ref IMasterTask masterTask, List<string> avoidTaskList, ref Guid masterGuid, Dictionary<Guid, IWrapperMasterClass> tasks);

        #region Private methods

        private void AvoidTasksPerUserOnKeyRemoved(Tuple<Guid, string> key)
        {
            _cancelledTasksPerUser.Remove(key);
        }

        private void SetJobsForWaitingAgents(object state)
        {
            lock (_lockGetJob)
            {
                int availableAgents;
                lock (_agentsPing.SyncLock)
                {
                    lock (_workingAgents)
                    {
                        availableAgents = _agentsPing.Count - _workingAgents.Count;
                    }
                }

                bool ignoreGatheringCondition = !_gatheringCondition && _gatheringCount >= 4;
                if (ignoreGatheringCondition)
                {
                    // Reset gathering permissivity
                    _gatheringCondition = true;
                    _gatheringCount = 0;
                }

                if (ignoreGatheringCondition || (_waitingAgents.Count >= availableAgents))
                {
                    var tasksToAvoidPerUser = new Dictionary<IAgent, List<string>>();

                    lock (_avoidTasksPerUser.SyncLock)
                        foreach (var agent in _waitingAgents.Keys)
                        {
                            IAgent agent1 = agent;
                            tasksToAvoidPerUser.Add(agent, _avoidTasksPerUser.Keys.Where(e => e.Item1 == agent1.Id).Select(e => e.Item2).ToList());
                        }

                    IMasterTask masterTask = null;

                    lock (_tasksLock)
                    {
                        while (true)
                        {
                            Guid masterGuid = Guid.Empty;
                            bool foundJob = GetNextMasterTask(ref masterTask, new List<string>(), ref masterGuid, _tasks);

                            if (masterTask != null && foundJob)
                            {
                                var remainingAgents = new Dictionary<IAgent, ManualResetEventSlim>();
                                foreach (var pair in _waitingAgents)
                                {
                                    var avoirTaskList = tasksToAvoidPerUser[pair.Key];
                                    if (avoirTaskList.Contains(masterTask.Name))
                                        continue;

                                    remainingAgents.Add(pair.Key, pair.Value);
                                }

                                if (remainingAgents.Any())
                                {
                                    // Logic for choosing the best suited agent for the selected master task
                                    var element = remainingAgents.OrderByDescending(e => e.Key.ProcessorCount).First();

                                    var descriptor = SetJobForAgent(element.Key, masterTask, masterGuid);
                                    _assignedTaskDescriptorsPerAgent.Add(element.Key, descriptor);
                                    _assignedMasterTasksPerAgent.Add(element.Key, masterTask);
                                    _waitingAgents.Remove(element.Key);
                                    element.Value.Set();
                                }
                                else
                                    break;
                            }
                            else
                            {
                                // There is no work so free the waiting agents
                                foreach (var pair in _waitingAgents)
                                {
                                    _assignedTaskDescriptorsPerAgent.Add(pair.Key, new TaskDescriptor());
                                    _assignedMasterTasksPerAgent.Add(pair.Key, null);
                                    pair.Value.Set();
                                }
                                _waitingAgents.Clear();
                                break;
                            }
                        }
                    }
                }
                else
                {
                    _gatheringCondition = false;
                    _gatheringCount++;
                }
            }
        }

        private TaskDescriptor SetJobForAgent(IAgent agent, IMasterTask masterTask, Guid masterGuid)
        {
            var descriptor = new TaskDescriptor {Enabled = false};
            // We must add the following section into the current 'taskslock' in to make sure that the masterTask doesnt get removed/killed
            try
            {
                descriptor.TypeName = masterTask.SlaveTypeName;
                descriptor.Id = masterTask.Id;
                descriptor.TypeAssemblyName = masterTask.TaskElement.SlaveTypeAssemblyName;
                descriptor.Job = masterTask.GetJob(agent);
                if (descriptor.Job != null)
                    descriptor.Job.TaskName = masterTask.Name;

                descriptor.Enabled = true;
            }
            catch (InvalidOperationException ex)
            {
                string jobId = descriptor.Job != null ? Convert.ToString(descriptor.Job.Id) : "Job is null";
                if (GridManager.EnableTrace)
                    Log.Warn("Unable to get Job for Job Id: " + jobId, ex);
            }
            catch (SocketException ex)
            {
                _tasks.Remove(masterGuid);
                ((IWrapperMasterClass) masterTask).FireFailedCompletion(new TaskException("GetDescriptor", ex).JsonException);
            }
            catch (RemotingException ex)
            {
                _tasks.Remove(masterGuid);
                ((IWrapperMasterClass) masterTask).FireFailedCompletion(new TaskException("GetDescriptor", ex).JsonException);
            }

            return descriptor;
        }

        private ManualResetEventSlim AddWaitingAgent(IAgent agent)
        {
            var waitEvent = new ManualResetEventSlim();
            lock (_lockGetJob)
            {
                // NOTE Check this : already existing item

                if (!_waitingAgents.ContainsKey(agent))
                    _waitingAgents.Add(agent, waitEvent);
            }
            return waitEvent;
        }

        private void RemoveWaitingAgent(IAgent agent, out IMasterTask masterTask, out TaskDescriptor descriptor)
        {
            lock (_lockGetJob)
            {
                descriptor = _assignedTaskDescriptorsPerAgent[agent];
                masterTask = _assignedMasterTasksPerAgent[agent];
                _assignedTaskDescriptorsPerAgent.Remove(agent);
                _assignedMasterTasksPerAgent.Remove(agent);
                _waitingAgents.Remove(agent);
            }
        }

        private void SetDistributionStat(IMasterTask masterTask, TaskDescriptor descriptor)
        {
            lock (_repositoryTasksDistribution)
            {
                var key = new Tuple<string, string>(descriptor.Job.TaskName, masterTask.TaskElement.MasterId);
                List<DateTime> distributions;
                if (!_repositoryTasksDistribution.TryGetValue(key, out distributions))
                {
                    distributions = new List<DateTime>();
                    _repositoryTasksDistribution.Add(key, distributions);
                }
                distributions.Add(DateTime.UtcNow);
            }
        }

        #endregion
    }
}