using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GridAgentSharedLib;
using GridAgentSharedLib.Clients;
using GridComputingSharedLib;

namespace GridSharedLibs
{
    /// <summary>
    ///     A master task implementation which makes sure that a collection of global job divisions is evenly distributed to
    ///     the working slaves.
    ///     These divisons are defined by user and added in the SlaveDivisons dictionary, where for each divison element a job
    ///     statement is
    ///     assigned from the following : Waiting, InTreatment, Done. OnStarting method is executed when the first slave calls
    ///     for getting a
    ///     job and thus the master job division is defined. 1°. When a slave returns his work results to the master and his
    ///     work is validated (cf. 3°),
    ///     the job division gets the status Done and the following job results for this divison, if any, are to be ignored.
    ///     Several distributions
    ///     of the same divison can be realised when an slave gets lost or when the master job is completed distributed and an
    ///     agent still ask it
    ///     for some more work. In both cases a divison could be redistributed again to the slaves. 2°. A master job is
    ///     considered to be finished
    ///     when all the job divisions have the status Done. This could occur whether or not all the slaves have returned their
    ///     work results.
    ///     When a master task is completed the grid manager takes it out of his active master tasks, so no more slave can ask
    ///     it for work.
    ///     Just before the master task is considered to be completed the user has the option to save the global master task
    ///     results since the
    ///     method OnSavingTaskResults is called. 3°. When a slave returns his work results these are validated by the user
    ///     implementation of
    ///     the SetWorkerJobState method. This one returns the status of the division.
    /// </summary>
    public sealed class DistribMasterTask : MasterTask
    {
        #region Fields

        private readonly IDistribImplementation _distribImplementation;
        private readonly IGridLog _log;
        private readonly ConcurrentDictionary<string, JobState> _slaveDivisons = new ConcurrentDictionary<string, JobState>();
        private readonly object _runningLock = new object();
        private readonly Dictionary<long, string> _slavesDistribution = new Dictionary<long, string>();
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private readonly Dictionary<IAgent, string> _workersDistribution = new Dictionary<IAgent, string>();
        private volatile int _initialisationStatus = InitializationStatus.NotInitialized;
        // TODO Check if we need to add this concept
        //private ExpiringDictionary<IAgent, object> _expiringWorkersDistribution = new ExpiringDictionary<IAgent,object>(TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(1));
        private long _taskCounter;
        private readonly Dictionary<object, int> _failedJoiningResultsPerJobDivision = new Dictionary<object, int>();
        private const int MaxFailedJoiningResultsCount = 3;

        #endregion

        public DistribMasterTask(IDistribImplementation distribImplementation, IGridLog log)
        {
            _log = log ?? GridLogManager.GetLogger(typeof (DistribMasterTask));
            _distribImplementation = distribImplementation;
        }

        #region Overrides

        public override string Result
        {
            get { return _distribImplementation.Result; }
        }

        public override bool AllJobsDispatched
        {
            get { return _slaveDivisons.Count > 0 && !_slaveDivisons.Values.Contains(JobState.Waiting); }
        }

        public override Job GetJob(IAgent agent)
        {
            lock (_runningLock)
            {
                if (InitialisationStatus == InitializationStatus.Initializing)
                    return null;

                if (Completed)
                {
                    if (EnableTrace)
                        _log.Warn(string.Format("{0} has an invalid state. Task is completed.", Name));

                    return null;
                }

                if (InitialisationStatus == InitializationStatus.NotInitialized)
                {
                    if (EnableTrace)
                        _log.Info("Master task not initialized. Initializing task ...");

                    _initialisationStatus = InitializationStatus.Initializing;

                    // Dont block the agents during the initialization phase.
                    // TODO Check if it's possible to block only one agent. Would add a "Wait for initialization" boolean on MasterTask implementation
                    // (The agent takes the master task, frees the lock and waits for the initialization. Careful if the initialization takes long time)
                    // Adding such option would make that a secondary agent will be redirected to another master task and so on. Could be ok for a Circular Job distribution...
                    // TODO One other option would be to add an option to "auto init" some master tasks by the GridManager (when they are added, by ex). The drawback is that 
                    // they could accumulate into memory if they arent consumed immediately.
                    new Action(Start).BeginInvoke(ar =>
                    {
                        if (_initialisationStatus == InitializationStatus.Initializing)
                        {
                            if (EnableTrace)
                                _log.Info("Master task initialized");

                            _initialisationStatus = InitializationStatus.Initialized;
                        }
                        if (_initialisationStatus == InitializationStatus.InitializationFailed)
                        {
                            if (EnableTrace)
                                _log.Info("Master task initialisation failed");
                        }
                    }, null);
                    return null;
                }

                if (InitialisationStatus != InitializationStatus.Initialized)
                    return null;

                // One agent can work on a job at a time. When one user asks for another job
                // we cancel the previous one.
                if (_workersDistribution.ContainsKey(agent))
                {
                    if (EnableTrace)
                        _log.Warn(string.Format("The agent already took some work for this task {0} .", Name));
                    RemoveAgentWork(agent);
                }

                if (_slaveDivisons.Values.Contains(JobState.Waiting)) // There is more work
                {
                    var res = GetNextJob(agent, JobState.Waiting);

                    if (EnableTrace)
                        _log.Info("Agent: " + (agent != null ? agent.MachineName : "Unknown agent") + " got a job.");
                    return res;
                }

                // Redispatch InTreatment divisons
                if (_distribImplementation.RedispatchInTreatmentJobs && _slaveDivisons.Values.Contains(JobState.InTreatment))
                    return GetNextJob(agent, JobState.InTreatment);

                if (EnableTrace)
                    _log.Warn(string.Format("{0} has an invalid state. There is no more work for this Task.", Name));

                return null;
            }
        }

        public override void Cancel(IAgent agent, long taskId)
        {
            if (EnableTrace)
                _log.Info("Cancel job request: " + agent.MachineName);

            lock (_runningLock)
            {
                RemoveAgentWork(agent);
            }

            base.Cancel(agent, taskId);
        }

        protected override void OnStarting()
        {
            try
            {
                var taskDivisions = _distribImplementation.StartInitTask(TaskElement.CustomProviderData);
                if (taskDivisions.Any())
                {
                    if (taskDivisions.Distinct().Count() != taskDivisions.Count)
                    {
                        const string message = "The job divisons arent distinct.";
                        _log.Warn(message);

                        _initialisationStatus = InitializationStatus.InitializationFailed;
                        throw new Exception(message);
                    }

                    if (EnableTrace)
                        _log.Info("There are " + taskDivisions.Count + " job divisions for this task.");

                    foreach (string taskDivision in taskDivisions)
                    {
                        _slaveDivisons.TryAdd(taskDivision, JobState.Waiting);
                    }

                    StepsGoal = taskDivisions.Count;
                }
                else
                {
                    _initialisationStatus = InitializationStatus.InitializedButNoWork;

                    if (EnableTrace)
                        _log.Info("No job division for this task. This master task will stop.");

                    Stop();
                    TriggerCompletion(true);
                }
            }
            catch
            {
                _initialisationStatus = InitializationStatus.InitializationFailed;
                throw;
            }

            _stopwatch.Start();
        }

        protected override void OnStopping()
        {
            _stopwatch.Stop();

            /* Perform any cleanup activities here. */

            _workersDistribution.Clear();
            _slavesDistribution.Clear();

            _slaveDivisons.Clear();

            if (EnableTrace)
            {
                _log.Info(string.Format("{0} is stopping...", Name));
                _log.InfoFormat("Task {0} lasted {1}", TaskElement.Name, _stopwatch.Elapsed);
            }
        }

        public override void Join(IAgent agent, TaskResult taskResult)
        {
            lock (_runningLock)
            {
                if (Completed)
                    return;

                string agentName = (agent != null ? agent.MachineName : "Unknown agent");

                string division;
                if (_slavesDistribution.TryGetValue(taskResult.JobId, out division))
                {
                    _slavesDistribution.Remove(taskResult.JobId);
                    if (agent != null)
                        _workersDistribution.Remove(agent);

                    if (_slaveDivisons[division] != JobState.Done)
                    {
                        var jobstate = JobState.Waiting;
                        try
                        {
                            //TODO Add an option to stop a task anytime when you consider that you got enough results (no real job division)
                            // Actually we can use TriggerCompletion when setting worker job state
                            if (_distribImplementation.SetWorkerJobState(taskResult, division))
                            {
                                jobstate = JobState.Done;
                                StepsCompleted++;
                            }
                            else
                            {
                                IncreaseSetJobFails(division, new Exception("This job division never completes"), agentName);
                            }
                        }
                        catch (Exception error)
                        {
                            IncreaseSetJobFails(division, error, agentName);
                        }

                        _slaveDivisons[division] = jobstate;

                        if (EnableTrace)
                            _log.Info("Joined results by : " + agentName + " .");
                    }
                    else
                    {
                        if (EnableTrace)
                            _log.Info("Joined results by : " + agentName + " . Results are ignored!");
                    }


                    if (EnableTrace)
                        _log.Info("Done : " + _slaveDivisons.Values.Count(e => e == JobState.Done) +
                                 " InTreatment : " + _slaveDivisons.Values.Count(e => e == JobState.InTreatment) +
                                 " Waiting : " + _slaveDivisons.Values.Count(e => e == JobState.Waiting));

                    if (_slaveDivisons.Values.All(el => el == JobState.Done))
                    {
                        if (EnableTrace)
                            _log.Info("All job divisions are done. Saving the results");

                        Exception error = null;

                        try
                        {
                            _distribImplementation.OnSavingTaskResults();
                        }
                        catch (Exception ex)
                        {
                            if (EnableTrace)
                                _log.Error("Saving task results failed", ex);

                            error = ex;
                        }

                        try
                        {
                            Stop();
                        }
// ReSharper disable once EmptyGeneralCatchClause
                        catch (Exception)
                        {
                        }

                        TriggerCompletion(true, error == null ? null : new TaskException("Join -> OnSavingResults failed", error));
                    }
                }
                else
                    throw new InvalidOperationException("TaskId doesnt belong to the current master task distribution");
            }
        }

        private void IncreaseSetJobFails(string division, Exception error, string agentName)
        {
            int failedCount = _failedJoiningResultsPerJobDivision.ContainsKey(division) ? _failedJoiningResultsPerJobDivision[division] : 0;
            if (failedCount > MaxFailedJoiningResultsCount)
            {
                try
                {
                    Stop();
                }
// ReSharper disable once EmptyGeneralCatchClause
                catch (Exception)
                {
                }

                TriggerCompletion(true, new TaskException("Join -> SetWorkerJobState failed " + MaxFailedJoiningResultsCount +
                                                          " times for this job division. Master task will stop since it ocuppies the grid", error));
            }

            failedCount++;
            _failedJoiningResultsPerJobDivision[division] = failedCount;

            if (EnableTrace)
                _log.Error("SetWorkerJobState failed. Agent: " + agentName, error);
        }

        public override void LostAgent(IAgent agent)
        {
            lock (_runningLock)
            {
                if (_initialisationStatus != InitializationStatus.Initialized)
                    return;

                RemoveAgentWork(agent);
                base.LostAgent(agent);
            }
        }

        #endregion

        #region Private methods

        private void RemoveAgentWork(IAgent agent)
        {
            if (!_workersDistribution.ContainsKey(agent))
                return;

            var distribution = _workersDistribution[agent];

            if (_slaveDivisons[distribution] != JobState.Done)
            {
                _slaveDivisons[distribution] = JobState.Waiting;

                if (EnableTrace)
                    _log.Info("Removed distributed work for agent: " + (agent != null ? agent.MachineName : "Unknown agent") + ".");
            }
        }

        private Job GetNextJob(IAgent agent, JobState state)
        {
            var job = new Job(_taskCounter);
            _taskCounter++;

            var pair = _slaveDivisons.First(el => el.Value == state);
            string jobData = pair.Key;

            try
            {
                job.CustomData = _distribImplementation.SetJob(jobData, agent);
            }
            catch (Exception error)
            {
                TriggerCompletion(true, new TaskException("GetNextJob -> _distribImplementation.SetJob data failed", error));
                throw;
            }

            _slaveDivisons[jobData] = JobState.InTreatment;
            _slavesDistribution[job.Id] = jobData;
            _workersDistribution[agent] = jobData;

            if (EnableTrace)
                _log.Info(string.Format("Username: {0} Machine: {1} dispatched JobId: {2} for task: {3} ", agent.UserName, agent.MachineName, job.Id, TaskElement.Name));

            return job;
        }

        #endregion

        public override int InitialisationStatus
        {
            get { return _initialisationStatus; }
        }
    }
}