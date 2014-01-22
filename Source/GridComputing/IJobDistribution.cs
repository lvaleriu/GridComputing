#region

using System;
using System.Collections.Generic;
using GridAgentSharedLib;
using GridAgentSharedLib.Clients;
using GridComputing.JobDistributions;
using GridComputingSharedLib;
using GridSharedLibs;

#endregion

namespace GridComputing
{
    public interface IJobDistribution
    {
        TaskDescriptor GetDescriptor(IAgent agent, out IMasterTask masterTask);
        void OnCheckCancelAbuse(IAgent agent, TaskInformation info);
        void SetComputingDistribution(Dictionary<string, Tuple<List<string>, double>> computingDistribution);
        void AddNewStatistics(string repositoryName, string masterId);
        void SetDivisonStats(IAgent agent, GridTaskElement taskElement, bool init, bool cancel = false);
        Dictionary<string, List<LightTaskExecutionStatistics>> GetTasksExecutionStatistics();
        GridDistributionStatistics GetGridStatistics();
    }
}