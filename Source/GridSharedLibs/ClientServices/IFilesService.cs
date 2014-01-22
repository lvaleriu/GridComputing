using GridSharedLibs.ServiceModel.Types;

namespace GridSharedLibs.ClientServices
{
    public interface IFilesService
    {
        FilesResponse GetFiles(string taskName);
    }
}