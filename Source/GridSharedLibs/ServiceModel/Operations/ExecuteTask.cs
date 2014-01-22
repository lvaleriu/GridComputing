#region

using ServiceStack.ServiceHost;

#endregion

namespace GridSharedLibs.ServiceModel.Operations
{
    [Route("/ExecuteTask")]
    public class ExecuteTask
    {
        public string MasterId { get; set; }
        public string SlaveId { get; set; }
        public string CustomData { get; set; }
    }
}