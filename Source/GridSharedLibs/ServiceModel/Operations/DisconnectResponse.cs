#region

using ServiceStack.ServiceInterface.ServiceModel;

#endregion

namespace GridSharedLibs.ServiceModel.Operations
{
    public class DisconnectResponse : IHasResponseStatus
    {
        public int Res { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }
}