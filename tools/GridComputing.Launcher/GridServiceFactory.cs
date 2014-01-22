using GridComputingServices;
using GridComputingServices.ClientServices;
using GridSharedLibs.ClientServices;

namespace GridComputing.Launcher
{
    public class GridServiceFactory : IGridServiceFactory
    {
        private readonly GridManager _gridManager;
        private IGridManagementService _gridManagementService;
        private IGridService _gridService;

        public GridServiceFactory(GridManager gridManager)
        {
            _gridManager = gridManager;
        }

        #region Implementation of IGridServiceFactory

        public IGridManagementService CreateGridManagementService(Config config)
        {
            return _gridManagementService ?? (_gridManagementService = new GridManagementService(config, _gridManager));
        }

        public IGridService CreateGridService()
        {
            return _gridService ?? (_gridService = new GridService(_gridManager));
        }

        #endregion

        #region Implementation of IDisposable

        public void Dispose()
        {
            
        }

        #endregion
    }
}