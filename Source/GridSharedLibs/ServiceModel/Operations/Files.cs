#region

using ServiceStack.ServiceHost;

#endregion

namespace GridSharedLibs.ServiceModel.Operations
{
    [Route("/files")]
    [Route("/files/{Path*}")]
    public class Files : IReturn<object>
    {
        public string Path { get; set; }
        public bool ForDownload { get; set; }
        public bool IsZipped { get; set; }
    }
}