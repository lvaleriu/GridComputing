#region

using ServiceStack.ServiceHost;

#endregion

namespace GridSharedLibs.ServiceModel.Operations
{
    [Route("/PublishTask")]
    public class PublishTask
    {
        public string Name { get; set; }
    }
}