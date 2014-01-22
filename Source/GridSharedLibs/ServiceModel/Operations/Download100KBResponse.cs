#region

using ServiceStack.ServiceInterface.ServiceModel;

#endregion

namespace GridSharedLibs.ServiceModel.Operations
{
    public class Download100KBResponse : IHasResponseStatus
    {
        public byte[] Res { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }
}