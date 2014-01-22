#region

using GridSharedLibs.ClientServices;
using GridSharedLibs.ServiceModel.Operations;
using ServiceStack.ServiceInterface;

#endregion

namespace GridComputingServices.Services
{
    public class JoinTaskService : Service
    {
        public IGridService GridService { get; set; }

        public JoinTaskResponse Any(JoinTask request)
        {
            return new JoinTaskResponse {Res = GridService.JoinTask(request.Agent, request.Result)};
        }
    }
}