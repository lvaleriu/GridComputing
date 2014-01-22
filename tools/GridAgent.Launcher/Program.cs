#region

using System;

#endregion

namespace GridAgent.Launcher
{
    internal static class Program
    {
        private static void Main()
        {
            string value = System.Configuration.ConfigurationManager.AppSettings.Get("EnableMasterCreatorsPing");
            TaskRunner.EnableMasterCreatorsPing = string.IsNullOrWhiteSpace(value) || Convert.ToBoolean(value);

            var appHost = new AppHost();
            appHost.Init();
            appHost.Start();

            Console.WriteLine("Press Q to exit the Recognition Grid Agent Service");

            while (true)
            {
                var key = Console.ReadKey();

                if (key.KeyChar == 'Q' || key.KeyChar == 'q')
                {
                    Console.WriteLine("Exiting ...");
                    appHost.Stop();
                    break;
                }
            }

            appHost.Stop();
        }
    }
}