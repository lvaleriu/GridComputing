#region

using Funq;
using GridComputingServices.ClientServices;
using GridComputingServices.Services;
using GridSharedLibs;
using GridSharedLibs.ClientServices;
using ServiceStack.Configuration;
using ServiceStack.Logging;
using ServiceStack.Logging.Support.Logging;
using ServiceStack.ServiceModel.Serialization;
using ServiceStack.WebHost.Endpoints;

#endregion

namespace GridComputingServices
{
    public class AppHost : AppHostHttpListenerBase
    {
        private static ILog _log;
        private readonly IGridServiceFactory _serviceFactory;
        private Container _container;

        public AppHost(IGridServiceFactory factory)
            : base("Grid management server", typeof (CancelJobService).Assembly)
        {
            _serviceFactory = factory;
#if DEBUG
            LogManager.LogFactory = new DebugLogFactory();
#else
            LogManager.LogFactory = new NullLogFactory();
            //LogManager.LogFactory = new ConsoleLogFactory();
#endif
            _log = LogManager.GetLogger(typeof (AppHost));
            _log.Info("AppHost constructed");
        }

        public override void Configure(Container container)
        {
            JsonDataContractSerializer.UseSerializer(new JsonNetSerializer());

            _container = new Container();
            _container.Register(_log);
            _container.Register(c => new Config(new AppSettings()));
            _container.Register(_serviceFactory);

            container.Register(_log);
            container.Register(c => new Config(new AppSettings()));
            GridService.Config = _container.Resolve<Config>();
        }

        public override void Start(string listeningAtUrlBase)
        {
            IGridManagementService managementService = _serviceFactory.CreateGridManagementService(Container.Resolve<Config>());
            IGridService service = _serviceFactory.CreateGridService();

            managementService.InitTaskRepository();

            _container.Register(managementService);
            _container.Register(service);

            Container.Register(managementService);
            Container.Register(service);

            base.Start(listeningAtUrlBase);

            _log.Info(string.Format("Web service will listen on: {0}", listeningAtUrlBase));
        }

        public override void Stop()
        {
            base.Stop();

            if (_serviceFactory != null)
                _serviceFactory.Dispose();
        }
    }
}