#region

using GridAgentSharedLib;
using ServiceStack.ServiceInterface.ServiceModel;

#endregion

namespace GridSharedLibs.ServiceModel.Operations
{
    public class StartNewJobResponse : IHasResponseStatus
    {
        public TaskDescriptor Descriptor { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }
}