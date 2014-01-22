#region

using GridSharedLibs.ClientServices;
using GridSharedLibs.ServiceModel.Operations;
using ServiceStack.ServiceInterface;

#endregion

namespace GridComputingServices.Services
{
    public class RegisterAgentService : Service
    {
        public IGridService GridService { get; set; }

        public RegisterResponse Any(RegisterAgent request)
        {
            return new RegisterResponse {Res = GridService.Register(request.Agent)};
        }
    }
}