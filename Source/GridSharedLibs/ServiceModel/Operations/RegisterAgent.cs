#region

using GridAgentSharedLib.Clients;
using ServiceStack.ServiceHost;

#endregion

namespace GridSharedLibs.ServiceModel.Operations
{
    [Route("/RegisterAgent")]
    public class RegisterAgent : IReturn<RegisterResponse>
    {
        public Agent Agent { get; set; }
    }
}