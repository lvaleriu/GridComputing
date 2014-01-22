#region

using GridComputingServices.ClientServices;
using GridSharedLibs.ClientServices;
using GridSharedLibs.ServiceModel.Operations;
using ServiceStack.ServiceInterface;

#endregion

namespace GridComputingServices.Services
{
    public class StartNewJobService : Service
    {
        public IGridService GridService { get; set; }
        public IGridManagementService GridManagementService { get; set; }

        #region Implementation of IService<StartNewJob>

        public StartNewJobResponse Any(StartNewJob request)
        {
            return new StartNewJobResponse {Descriptor = GridService.StartNewJob(request.Agent)};
        }

        #endregion

        public ScheduleTaskResponse Any(ScheduleTask request)
        {
            GridManagementService.ScheduleTask(request.Name, request.MasterId, request.SlaveId, request.CronExpression, request.CustomData);

            return new ScheduleTaskResponse();
        }
    }
}