using System;
using GridSharedLibs.ClientServices;

namespace GridComputingServices.ClientServices
{
    public interface IGridServiceFactory : IDisposable
    {
        IGridManagementService CreateGridManagementService(Config config);
        IGridService CreateGridService();
    }
}