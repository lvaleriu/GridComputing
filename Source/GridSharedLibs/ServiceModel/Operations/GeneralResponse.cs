#region

using ServiceStack.ServiceInterface.ServiceModel;

#endregion

namespace GridSharedLibs.ServiceModel.Operations
{
    public class GeneralResponse : IHasResponseStatus
    {
        public GeneralResponse()
        {
            ResponseStatus = new ResponseStatus();
        }

        public ResponseStatus ResponseStatus { get; set; }
    }
}