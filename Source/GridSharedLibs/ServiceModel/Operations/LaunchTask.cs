#region

using ServiceStack.ServiceHost;

#endregion

namespace GridSharedLibs.ServiceModel.Operations
{
    [Route("/LaunchTask")]
    public class LaunchTask
    {
        public string Name { get; set; }
        public string MasterId { get; set; }
        public string SlaveId { get; set; }
        public string CustomData { get; set; }
    }
}