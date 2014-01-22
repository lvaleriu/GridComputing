#region

using ServiceStack.ServiceInterface.ServiceModel;

#endregion

namespace GridSharedLibs.ServiceModel.Types
{
    public class FilesResponse : IHasResponseStatus
    {
        public FolderResult Directory { get; set; }

        public FileResult File { get; set; }

        #region IHasResponseStatus Members

        public ResponseStatus ResponseStatus { get; set; }

        #endregion
    }
}