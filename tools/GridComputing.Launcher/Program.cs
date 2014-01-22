#region

using System;
using GridComputingServices;
using ServiceStack.Configuration;

#endregion

namespace GridComputing.Launcher
{
    internal static class Program
    {
        private static AppHost _appHost;

        public static void Main()
        {
            var config = new GridComputingServices.Config(new AppSettings());
            GridManager.EnableTrace = config.EnableTrace;
            GridManager.EnableMasterCreatorsPing = config.EnableMasterCreatorsPing;
            GridManager.CheckCancelAbuse = config.CheckCancelAbuse;
            GridManager.UseIpcChannel = config.UseIpcChannel;

            _appHost = new AppHost(new GridServiceFactory(new GridManager(TimeSpan.FromSeconds(config.ExpirationPeriodSeconds), TimeSpan.FromSeconds(config.ExpirationCheckingIntervalSeconds))));

            _appHost.Init();
            _appHost.Start(config.ListeningUrl);

            Console.WriteLine("Grid computing server listening on :" + config.ListeningUrl);
            Console.WriteLine("Press Q to exit the Recognition Grid Computing Service");

            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey();

                if (key.KeyChar == 'Q' || key.KeyChar == 'q')
                {
                    Console.WriteLine("Exiting ...");
                    break;
                }
            }

            Console.WriteLine("Stopping host...");
            _appHost.Stop();
            Console.WriteLine("Host stopped");
        }
    }
}