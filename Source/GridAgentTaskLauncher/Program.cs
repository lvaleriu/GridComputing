#region

using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters;
using GridSharedLibs;
using Newtonsoft.Json;

#endregion

namespace GridAgentTaskLauncher
{
    internal class Program
    {
        private const bool LogInfo = false;

        public static void Main(string[] args)
        {
            if (args == null || args.Length == 0)
            {
#if !TEST
                args = new[] { "test", "20001", "1" };
                //args = new[] { Guid.NewGuid().ToString().Replace("-", ""), "20001", "1" };
                //args = new[] { "AppHost", "10001", "0" };
                //args = new[] { "http://localhost", "10001", "2" };
#else
                throw new Exception("You must specify the server uri!");
#endif
            }

            bool isIpc = (args[2] == "1");
            bool isHttp = (args[2] == "2");
            string serverUri = args[0];
            string port = isIpc ? args[0] : args[1];

            if (isHttp)
            {
                var appHost = new ProcessAppHost();
                appHost.Init();
                string listeningUrl = string.Format("{0}:{1}/", serverUri, port);
                Console.WriteLine("Task launcher process server listening on :" + listeningUrl);
                appHost.Start(listeningUrl);

                Console.WriteLine("Please enter to stop the server");
                Console.ReadLine();
                appHost.Stop();
            }
            else
            {
                try
                {
                    var channel = isIpc
                        ? RemotingUtil.CreateIpcChannel(port)
                        : (IChannel) RemotingUtil.CreateTcpChannel(port);


                    // Register the server channel.
                    ChannelServices.RegisterChannel(channel, false);

                    if (LogInfo)
                    {
                        // Show the name of the channel.
                        Console.WriteLine("The name of the channel is {0}.", channel.ChannelName);

                        // Show the priority of the channel.
                        Console.WriteLine("The priority of the channel is {0}.", channel.ChannelPriority);

                        // Show the URIs associated with the channel.
                        var channelData = (ChannelDataStore) ((IChannelReceiver) channel).ChannelData;
                        foreach (string uri in channelData.ChannelUris)
                        {
                            Console.WriteLine("The channel URI is {0}.", uri);
                        }
                    }

                    Console.WriteLine("Registering remote {0} server uri: {1} port : {2}", isIpc ? "IPC" : "TCP", serverUri, port);

                    serverUri = isIpc ? "TaskLauncherServer" : serverUri;
                    // Expose an object for remote calls.
                    RemotingConfiguration.RegisterWellKnownServiceType(typeof(TaskLauncherServer), serverUri, WellKnownObjectMode.Singleton);

                    if (LogInfo)
                    {
                        // Parse the channel's URI. 
                        string[] urls = ((IChannelReceiver) channel).GetUrlsForUri(serverUri);
                        if (urls.Length > 0)
                        {
                            string objectUrl = urls[0];
                            string objectUri;
                            string channelUri = channel.Parse(objectUrl, out objectUri);
                            Console.WriteLine("The object URI is {0}.", objectUri);
                            Console.WriteLine("The channel URI is {0}.", channelUri);
                            Console.WriteLine("The object URL is {0}.", objectUrl);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }

                Console.WriteLine("Please enter to stop the server");
                Console.ReadLine();
            }
        }
    }
}