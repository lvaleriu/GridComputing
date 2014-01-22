#region

using GridAgentSharedLib.Clients;
using ServiceStack.ServiceHost;

#endregion

namespace GridSharedLibs.ServiceModel.Operations
{
    [Route("/Ping")]
    public class Ping : IReturn<GeneralResponse>
    {
        public Agent Agent { get; set; }
    }
}