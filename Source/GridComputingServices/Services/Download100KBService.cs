#region

using GridSharedLibs.ClientServices;
using GridSharedLibs.ServiceModel.Operations;
using ServiceStack.ServiceInterface;

#endregion

namespace GridComputingServices.Services
{
    public class Download100KBService : Service
    {
        public IGridService GridService { get; set; }

        public Download100KBResponse Any(Download100KB request)
        {
            return new Download100KBResponse {Res = GridService.Download100KB()};
        }
    }
}