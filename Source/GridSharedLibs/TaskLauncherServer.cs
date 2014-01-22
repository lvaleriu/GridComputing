#region

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using GridAgentSharedLib;
using GridAgentSharedLib.TypesCreation;
using GridComputingSharedLib;
using GridComputingSharedLib.TypesCreation;
using Newtonsoft.Json;

#endregion

namespace GridSharedLibs
{
    public class TaskLauncherServer : MarshalByRefObject, ISharedTaskInfo
    {
        #region Fields

        private bool _pingCheck = true;
        private readonly object _lockInstance = new object();
        private bool _connected;

        private int _count;
        private DateTime _lastPingTime;
        private MasterAppDomainCreateInstantiator<MasterTask> _masterAppDomain;
        private Timer _pingCallbackTimer;
        private BaseSlaveCreateInstantiator<SlaveTask> _slaveAppDomain;

        #endregion

        #region Overrides

        public IMasterTask CreateMasterTask(string assemblyQualifiedName, string dllLocation)
        {
            Console.WriteLine("Create Master Task : " + assemblyQualifiedName);

            lock (_lockInstance)
            {
                if (_masterAppDomain == null)
                    _masterAppDomain = new MasterAppDomainCreateInstantiator<MasterTask>("new master domain", new FileInfo(dllLocation).Directory.FullName);

                return new WrapperMasterClass(_masterAppDomain.CreateMasterTask(assemblyQualifiedName, dllLocation));
            }
        }

        public IFullMasterTask CreateFullMasterTask(string assemblyQualifiedName, string dllLocation)
        {
            Console.WriteLine("Create Full Master Task : " + assemblyQualifiedName);

            lock (_lockInstance)
            {
                if (_masterAppDomain == null)
                    _masterAppDomain = new MasterAppDomainCreateInstantiator<MasterTask>("new master domain", new FileInfo(dllLocation).Directory.FullName);

                return new WrapperFullMasterClass(_masterAppDomain.CreateFullMasterTask(assemblyQualifiedName, dllLocation));
            }
        }

        public ISlaveTask CreateSlaveTask(string assemblyQualifiedName, string dllLocation)
        {
            lock (_lockInstance)
            {
                if (_slaveAppDomain == null)
                {
                    string directoryPath = new FileInfo(dllLocation).Directory.FullName;
                    var instance = new SlaveCreateInstantiator<SlaveTask>();
                    instance.SetDirPath(directoryPath);
                    _slaveAppDomain = instance;
                }

                return new WrapperSlaveClass(_slaveAppDomain.CreateSlaveTask(assemblyQualifiedName, dllLocation));
            }
        }

        public override object InitializeLifetimeService()
        {
            return null;
        }

        #endregion

        #region Implementation of ISharedTaskInfo

        public void Ping()
        {
            if (!_connected)
            {
                _connected = true;
                _pingCallbackTimer = new Timer(PingCheckFunc, null, TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(1));
            }

            _lastPingTime = DateTime.UtcNow;
        }

        public void Log(string message)
        {
            Console.WriteLine(message);
        }

        public void SetPingChecking(bool enable)
        {
            Console.WriteLine("Ping checking is : " + (enable ? "enabled" : "disabled"));
            _pingCheck = enable;
        }

        public void Close()
        {
            if (_pingCheck && _pingCallbackTimer != null)
                _pingCallbackTimer.Change(Timeout.Infinite, Timeout.Infinite);

            Environment.Exit(0);
        }

        public int GetProcessId()
        {
            return Process.GetCurrentProcess().Id;
        }

        public AppDomain GetExecutingDomain()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Private methods

        private void PingCheckFunc(object state)
        {
            if (!_pingCheck)
                return;

            if (_lastPingTime < DateTime.UtcNow.AddSeconds(-2))
                IncreaseFailingCount();
            else
            {
                _count = 0;
            }
        }

        private void IncreaseFailingCount()
        {
            _count++;

            if (_count == 4)
            {
                Close();
            }
        }

        #endregion
    }
}