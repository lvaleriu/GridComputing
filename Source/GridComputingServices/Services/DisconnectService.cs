#region

using GridSharedLibs.ClientServices;
using GridSharedLibs.ServiceModel.Operations;
using ServiceStack.ServiceInterface;

#endregion

namespace GridComputingServices.Services
{
    public class DisconnectService : Service
    {
        public IGridService GridService { get; set; }

        public DisconnectResponse Any(Disconnect request)
        {
            return new DisconnectResponse {Res = GridService.Disconnect(request.Agent)};
        }
    }
}