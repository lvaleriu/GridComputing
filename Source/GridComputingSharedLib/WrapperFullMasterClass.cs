using System.Collections.Generic;
using GridAgentSharedLib;
using GridAgentSharedLib.Clients;

namespace GridComputingSharedLib
{
    public class WrapperFullMasterClass : WrapperMasterClass, IWrapperFullMasterClass
    {
        private readonly IFullMasterTask _masterTask;

        public WrapperFullMasterClass(IFullMasterTask masterTask) : base(masterTask)
        {
            _masterTask = masterTask;
        }

        public bool RedispatchInTreatmentJobs { get; private set; }

        public bool SetWorkerJobState(TaskResult taskResult, string taskData)
        {
            return _masterTask.SetWorkerJobState(taskResult, taskData);
        }

        public void OnSavingTaskResults()
        {
            _masterTask.OnSavingTaskResults();
        }

        public List<string> StartInitTask(string customProviderData)
        {
            return _masterTask.StartInitTask(customProviderData);
        }

        public string SetJob(string jobData, IAgent agent)
        {
            return _masterTask.SetJob(jobData, agent);
        }
    }
}