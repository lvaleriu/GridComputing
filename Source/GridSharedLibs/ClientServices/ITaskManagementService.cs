using GridSharedLibs.ServiceModel.Operations;

namespace GridSharedLibs.ClientServices
{
    public  interface ITaskManagementService
    {
        GetGridTasksResponse GetGridTasks(string taskName);
    }
}