#region

using System;
using GridSharedLibs.ClientServices;
using GridSharedLibs.ServiceModel.Operations;
using ServiceStack.Logging;
using ServiceStack.ServiceInterface;

#endregion

namespace GridComputingServices.Services
{
    public class CancelJobService : Service
    {
        protected static ILog Log = LogManager.GetLogger(typeof (CancelJobService));
        public IGridService GridService { get; set; }

        public GeneralResponse Any(CancelJob request)
        {
            var response = new GeneralResponse();

            try
            {
                int res = GridService.CancelJob(request.Agent, request.Info);
            }
            catch (Exception ex)
            {
                Log.Error("CancelJob post", ex);
            }

            return response;
        }
    }
}