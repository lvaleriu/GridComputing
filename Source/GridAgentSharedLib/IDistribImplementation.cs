using System.Collections.Generic;
using GridAgentSharedLib.Clients;

namespace GridAgentSharedLib
{
    public interface IDistribImplementation
    {
        bool RedispatchInTreatmentJobs { get; }
        string Result { get; }

        bool SetWorkerJobState(TaskResult taskResult, string taskData);
        void OnSavingTaskResults();
        List<string> StartInitTask(string customProviderData);
        string SetJob(string jobData, IAgent agent);
    }
}