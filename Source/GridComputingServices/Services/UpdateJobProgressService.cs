#region

using GridSharedLibs.ClientServices;
using GridSharedLibs.ServiceModel.Operations;
using ServiceStack.ServiceInterface;

#endregion

namespace GridComputingServices.Services
{
    public class UpdateJobProgressService : Service
    {
        public IGridService GridService { get; set; }

        public UpdateJobProgressResponse Any(UpdateJobProgress request)
        {
            return new UpdateJobProgressResponse {Res = GridService.UpdateJobProgress(request.Agent, request.TaskProgress)};
        }
    }
}