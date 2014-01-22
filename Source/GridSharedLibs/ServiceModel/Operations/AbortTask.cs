#region

using ServiceStack.ServiceHost;

#endregion

namespace GridSharedLibs.ServiceModel.Operations
{
    [Route("/AbortTask")]
    [Route("/AbortTask/{MasterId}")]
    public class AbortTask : IReturn<GeneralResponse>
    {
        public string MasterId { get; set; }
    }
}