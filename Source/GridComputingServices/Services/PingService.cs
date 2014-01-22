#region

using GridSharedLibs.ClientServices;
using GridSharedLibs.ServiceModel.Operations;
using ServiceStack.ServiceInterface;

#endregion

namespace GridComputingServices.Services
{
    public class PingService : Service
    {
        public IGridService GridService { get; set; }

        public GeneralResponse Any(Ping request)
        {
            int res = GridService.Ping(request.Agent);
            return new GeneralResponse();
        }
    }
}