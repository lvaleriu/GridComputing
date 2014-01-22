#region

using System;
using GridComputingServices.ClientServices;
using GridSharedLibs.ServiceModel.Operations;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.ServiceModel;

#endregion

namespace GridComputingServices.Services
{
    public class GridServices : Service
    {
        public IGridManagementService GridManagementService { get; set; }

        public GeneralResponse Any(AbortTask task)
        {
            try
            {
                GridManagementService.AbortTask(task.MasterId);

                return new GeneralResponse();
            }
            catch (Exception ex)
            {
                return new GeneralResponse {ResponseStatus = new ResponseStatus(ex.Message, ex.Message)};
            }
        }
    }
}