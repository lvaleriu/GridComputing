using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.ServiceModel;

namespace GridSharedLibs.ServiceModel.Operations
{
    public class ScheduleTask : IReturn<ScheduleTaskResponse>
    {
        public string Name { get; set; }
        public string MasterId { get; set; }
        public string SlaveId { get; set; }
        public string CronExpression { get; set; }
        public string CustomData { get; set; }
    }

    public class ScheduleTaskResponse : IHasResponseStatus
    {
        public ResponseStatus ResponseStatus { get; set; }
    }
}