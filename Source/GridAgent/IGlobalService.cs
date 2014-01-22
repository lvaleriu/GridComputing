using GridSharedLibs.ClientServices;

namespace GridAgent
{
    public interface IGlobalService : IGridService, IFilesService, ITaskManagementService
    {
        
    }
}