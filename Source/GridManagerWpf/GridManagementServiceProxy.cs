#region

using System;
using System.IO;
using GridAgentSharedLib.Clients;
using GridComputing;
using GridComputingServices.ClientServices;
using GridSharedLibs.ServiceModel.Operations;
using ServiceStack.ServiceClient.Web;

#endregion

namespace GridManagerWpf
{
    public class GridManagementServiceProxy : IGridManagementService
    {
        private readonly ServiceClientBase _clientBase;

        public GridManagementServiceProxy(ServiceClientBase clientBase)
        {
            _clientBase = clientBase;
        }

        #region Implementation of IGridManagementService

        public GridSummary GetGridSummary(Client client)
        {
            return _clientBase.Send<GridSummary>(new GetGridSummary {Client = client});
        }

        public AddTaskLibrariesResponse AddTaskLibraries(string name, string zipFilePath)
        {
            return _clientBase.PostFileWithRequest<AddTaskLibrariesResponse>("PublishTask", new FileInfo(zipFilePath), new PublishTask {Name = name});
        }

        #endregion

        public GetGridTasksResponse GetGridTasks(string name)
        {
            return _clientBase.Send(new GetGridTaks());
        }

        public GeneralResponse RemoveTaskRepository(string name)
        {
            return _clientBase.Send<GeneralResponse>(new RemoveTaskRepository {Name = name});
        }

        public void ScheduleTask(string repositoryName, string masterId, string slaveId, string cronExpression, string customData)
        {
            _clientBase.Send(new ScheduleTask
            {
                CronExpression = cronExpression, 
                Name = repositoryName, 
                MasterId = masterId, 
                SlaveId = slaveId,
                CustomData = customData
            });
        }

        public AddTaskLibrariesResponse LaunchTask(string masterId, string slaveId, string customData)
        {
            return _clientBase.Send<AddTaskLibrariesResponse>(new LaunchTask {MasterId = masterId, SlaveId = slaveId, CustomData = customData});
        }

        public void InitTaskRepository()
        {
            throw new NotImplementedException();
        }

        public GeneralResponse AbortTask(string masterTask)
        {
            return _clientBase.Send(new AbortTask {MasterId = masterTask});
        }

        public ExecuteTaskResponse ExecuteTask(string masterId, string slaveId, string customData)
        {
            return _clientBase.Send<ExecuteTaskResponse>(new ExecuteTask {MasterId = masterId, SlaveId = slaveId, CustomData = customData});
        }
    }
}