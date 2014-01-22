using System.ServiceModel;
using GridAgentSharedLib.Clients;
using GridComputing;
using GridSharedLibs.ServiceModel.Operations;

namespace GridComputingServices.ClientServices
{
    /// <summary>
    /// Provides service methods to Grid Management applications.
    /// </summary>
    [ServiceContract]
    public interface IGridManagementService
    {
        /// <summary>
        /// Gets the grid info representing the current state
        /// of the grid.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <returns>A summary of the current state 
        /// of the Grid.</returns>
        [OperationContract]
        [ServiceKnownType(typeof(Client))]
        GridSummary GetGridSummary(Client client);

        AddTaskLibrariesResponse AddTaskLibraries(string name, string zipFilePath);
        GetGridTasksResponse GetGridTasks(string name);

        GeneralResponse RemoveTaskRepository(string name);

        void ScheduleTask(string repositoryName, string masterId, string slaveId, string cronExpression, string customData);

        AddTaskLibrariesResponse LaunchTask(string masterId, string slaveId, string customData);
        void InitTaskRepository();
        GeneralResponse AbortTask(string masterTask);
        ExecuteTaskResponse ExecuteTask(string masterId, string slaveId, string customData);
    }
}