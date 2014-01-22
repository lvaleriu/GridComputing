#region

using ServiceStack.ServiceInterface.ServiceModel;

#endregion

namespace GridSharedLibs.ServiceModel.Operations
{
    public class UpdateJobProgressResponse : IHasResponseStatus
    {
        public int Res { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }
}