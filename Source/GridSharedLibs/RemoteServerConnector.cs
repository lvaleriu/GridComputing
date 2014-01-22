#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Threading;
using System.Threading.Tasks;
using GridAgentSharedLib;
using GridAgentSharedLib.TypesCreation;
using GridComputingSharedLib;
using GridComputingSharedLib.TypesCreation;
using Newtonsoft.Json;

#endregion

namespace GridSharedLibs
{
    public class RemoteServerConnector : ICreateMasterInstance, ICreateSlaveInstance, ILogWriter
    {
        #region Fields

        private static readonly SortedSet<int> UsedPorts = new SortedSet<int>();
        private static readonly object UserPortsLock = new object();
        private readonly bool _enableMasterCreatorsPing;
        private readonly object _instanceLock = new object();
        private readonly bool _isClient;
        private readonly string _serverUri;
        private readonly bool _useIpcChannel;
        private IChannel _channel;
        private int _currentPort;
        private bool _exitThread;
        private volatile bool _launchingSlaveProcess;
        private ISharedTaskInfo _sharedTaskInfo;
        private Process _slaveLauncherProcess;
        private Thread _threadTimer;

        #endregion

        static RemoteServerConnector()
        {
            //Log = LogManager.GetLogger(typeof(RemoteServerConnector));
        }

        public RemoteServerConnector(string serverUri, bool useIpcChannel, bool isClient, bool enableMasterCreatorsPing = true)
        {
            _serverUri = serverUri;
            _useIpcChannel = useIpcChannel;
            _isClient = isClient;
            _enableMasterCreatorsPing = enableMasterCreatorsPing;
        }

        public void Log(string message)
        {
            lock (_instanceLock)
            {
                if (_sharedTaskInfo != null)
                    _sharedTaskInfo.Log(message);
            }
        }

        public event Action<ICreateMasterInstance> RemoteServerClosed;

        private void Start()
        {
            _exitThread = false;

            Thread.Sleep(TimeSpan.FromSeconds(1));

            while (!_exitThread)
            {
                OnSlaveLauncherTimerCallback();
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
        }

        #region Private methods

        private void InitRemoteServer(string directoryPath)
        {
            lock (_instanceLock)
            {
                if (_sharedTaskInfo == null)
                {
                    InitNewRemoteServer(directoryPath);

                    _threadTimer = new Thread(Start) {Priority = ThreadPriority.Highest};
                    _threadTimer.Start();
                }
            }
        }

        private void InitNewRemoteServer(string directoryPath)
        {
            int port = GetPort();
            _currentPort = port;

            //Check if there is an existing process listening for incoming connections
            try
            {
                var processList = Process.GetProcessesByName("GridAgentTaskLauncher.exe");
                if (processList.Any())
                {
                    ISharedTaskInfo taskInfo = ConnectToRemoteServer(_serverUri, port);
                    var procId = taskInfo.GetProcessId();
                    taskInfo.SetPingChecking(_enableMasterCreatorsPing);

                    _slaveLauncherProcess = Process.GetProcessById(procId);
                    _sharedTaskInfo = taskInfo;

                    return;
                }
            }
// ReSharper disable once EmptyGeneralCatchClause
            catch
            {
            }

            var processStartInfo = new ProcessStartInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GridAgentTaskLauncher.exe"),
                string.Format("{0} {1} {2}",
                    _serverUri,
                    port,
                    _useIpcChannel ? "1" : "0"))
            {
                WorkingDirectory = directoryPath
            };
            _slaveLauncherProcess = Process.Start(processStartInfo);

            int retryCount = 0;
            while (retryCount < 4)
            {
                Thread.Sleep(100);

                retryCount++;
                try
                {
                    ISharedTaskInfo taskInfo = ConnectToRemoteServer(_serverUri, port);
                    var procId = taskInfo.GetProcessId();
                    taskInfo.SetPingChecking(_enableMasterCreatorsPing);

                    _slaveLauncherProcess = Process.GetProcessById(procId);
                    _sharedTaskInfo = taskInfo;

                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Connection to remote task launcher server failed. Retrying! Ex: " + JsonConvert.SerializeObject(ex, Formatting.Indented));
                }
            }

            throw new Exception("Couldnt connect to the process!");
        }

        private List<int> GetUsedPorts()
        {
            var usedPorts = new List<int>();

            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();

            foreach (TcpConnectionInformation tcpInfo in tcpConnInfoArray)
            {
                usedPorts.Add(tcpInfo.LocalEndPoint.Port);
            }

            return usedPorts;
        }

        private int GetPort()
        {
            var usedPorts = GetUsedPorts();

            int port;
            lock (UserPortsLock)
            {
                port = UsedPorts.LastOrDefault();
                if (port != 0)
                {
                    port++;
                }
                else
                {
                    port = _isClient ? 10001 : 20001;
                }

                while (usedPorts.Contains(port))
                {
                    port++;
                }

                UsedPorts.Add(port);
            }
            return port;
        }

        private ISharedTaskInfo ConnectToRemoteServer(string serverUri, int port)
        {
            // name : "ipc client"
            _channel = ChannelServices.GetChannel(_useIpcChannel ? serverUri : "tcp");

            if (_channel == null)
            {
                _channel = _useIpcChannel
                    ? RemotingUtil.CreateIpcChannelWithUniquePortName()
                    : RemotingUtil.CreateClientTcpChannel();

                ChannelServices.RegisterChannel(_channel, false);
            }

            string remoteServer = !_useIpcChannel
                ? string.Format("tcp://{2}:{0}/{1}", port, serverUri, Environment.MachineName)
                : string.Format("ipc://{0}/TaskLauncherServer", serverUri);

            Console.WriteLine("Connecting to remote server : {0}", remoteServer);

            return (ISharedTaskInfo) Activator.GetObject(typeof (ISharedTaskInfo), remoteServer);
        }

        private void CloseProcess()
        {
            try
            {
                if (_slaveLauncherProcess != null && !_slaveLauncherProcess.HasExited)
                    _slaveLauncherProcess.Kill();
            }
            catch (Exception ex)
            {
                //Log.Error("CloseProcess", ex);
                Console.WriteLine("CloseProcess: {0}", ex);
            }

            _slaveLauncherProcess = null;
        }

        private void CloseServer()
        {
            if (_sharedTaskInfo != null)
            {
                CloseProcess();

                if (RemoteServerClosed != null)
                    RemoteServerClosed(this);
            }
            _sharedTaskInfo = null;

            lock (UserPortsLock)
            {
                UsedPorts.Remove(_currentPort);
            }
        }

        private void ExecuteAction(Action action, string name)
        {
            var waitEvent = new ManualResetEvent(false);
            Exception exception = null;

            Task.Factory.StartNew(() =>
            {
                try
                {
                    action();
                }
                catch (AggregateException)
                {
                }
                catch (SocketException ex)
                {
                    exception = ex;

                    if (RemoteServerClosed != null)
                        RemoteServerClosed(this);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
                waitEvent.Set();
            });

            if (!waitEvent.WaitOne(TimeSpan.FromSeconds(1)))
            {
                Console.WriteLine("Action {0} lasted too much!", name);
                return;
            }

            if (exception != null)
                throw exception;
        }

        private void OnSlaveLauncherTimerCallback()
        {
            if (_launchingSlaveProcess)
                return;

            _launchingSlaveProcess = true;

            lock (_instanceLock)
            {
                try
                {
                    if (_sharedTaskInfo != null && _slaveLauncherProcess != null && !_slaveLauncherProcess.HasExited)
                        ExecuteAction(() => _sharedTaskInfo.Ping(), "PingTest");
                    else
                    {
                        _exitThread = true;
                        _threadTimer = null;

                        if (RemoteServerClosed != null)
                            RemoteServerClosed(this);
                    }
                }
                catch (RemotingException ex)
                {
                    Console.WriteLine(ex);
                }
                catch (SocketException)
                {
                }
            }

            _launchingSlaveProcess = false;
        }

        #endregion

        #region ICreateMasterInstance implementation

        public void Close()
        {
            lock (_instanceLock)
            {
                _exitThread = true;
                _threadTimer = null;

                try
                {
                    if (ChannelServices.RegisteredChannels.Contains(_channel))
                        ChannelServices.UnregisterChannel(_channel);
                }
// ReSharper disable once EmptyGeneralCatchClause
                catch
                {
                }

                CloseServer();
            }
        }

        public AppDomain GetExecutingDomain()
        {
            lock (_instanceLock)
            {
                if (_sharedTaskInfo == null)
                    return null;

                return _sharedTaskInfo.GetExecutingDomain();
            }
        }

        public int GetProcessId()
        {
            lock (_instanceLock)
            {
                if (_sharedTaskInfo == null)
                    return -1;

                return _sharedTaskInfo.GetProcessId();
            }
        }

        public IMasterTask CreateMasterTask(string assemblyQualifiedName, string dllLocation)
        {
            lock (_instanceLock)
            {
                InitRemoteServer(new FileInfo(dllLocation).Directory.FullName);

                try
                {
                    return _sharedTaskInfo.CreateMasterTask(assemblyQualifiedName, dllLocation);
                }
                catch (SocketException)
                {
                    Close();

                    throw;
                }
            }
        }

        public IFullMasterTask CreateFullMasterTask(string assemblyQualifiedName, string dllLocation)
        {
            lock (_instanceLock)
            {
                InitRemoteServer(new FileInfo(dllLocation).Directory.FullName);

                try
                {
                    return _sharedTaskInfo.CreateFullMasterTask(assemblyQualifiedName, dllLocation);
                }
                catch (SocketException)
                {
                    Close();

                    throw;
                }
            }
        }

        public ISlaveTask CreateSlaveTask(string assemblyQualifiedName, string dllLocation)
        {
            lock (_instanceLock)
            {
                InitRemoteServer(new FileInfo(dllLocation).Directory.FullName);

                try
                {
                    return _sharedTaskInfo.CreateSlaveTask(assemblyQualifiedName, dllLocation);
                }
                catch (SocketException)
                {
                    Close();

                    throw;
                }
            }
        }

        #endregion
    }
}