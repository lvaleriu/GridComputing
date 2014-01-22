#region

using System;
using System.IO;
using GridComputingServices.ClientServices;
using GridSharedLibs.ServiceModel.Operations;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.ServiceModel;

#endregion

namespace GridComputingServices.Services
{
    public class GetGridSummaryService : Service
    {
        public IGridManagementService GridManagementService { get; set; }
        public Config Config { get; set; }

        public object Any(GetGridSummary request)
        {
            return GridManagementService.GetGridSummary(request.Client);
        }

        public ExecuteTaskResponse Any(ExecuteTask request)
        {
            try
            {
                return GridManagementService.ExecuteTask(request.MasterId, request.SlaveId, request.CustomData);
            }
            catch (Exception ex)
            {
                return new ExecuteTaskResponse {ResponseStatus = new ResponseStatus(ex.Message, ex.Message)};
            }
        }

        public GetGridTasksResponse Any(GetGridTaks request)
        {
            return GridManagementService.GetGridTasks(request.TaskName);
        }

        public AddTaskLibrariesResponse Any(LaunchTask request)
        {
            return GridManagementService.LaunchTask(request.MasterId, request.SlaveId, request.CustomData);
        }

        public AddTaskLibrariesResponse Any(PublishTask request)
        {
            var zipFile = Request.Files[0];
            string tempZipFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".zip");
            zipFile.SaveTo(tempZipFile);

            return GridManagementService.AddTaskLibraries(request.Name, tempZipFile);
        }

        public GeneralResponse Any(RemoveTaskRepository request)
        {
            try
            {
                return GridManagementService.RemoveTaskRepository(request.Name);
            }
            catch (Exception ex)
            {
                return new GeneralResponse {ResponseStatus = new ResponseStatus("400", ex.Message)};
            }
        }
    }
}