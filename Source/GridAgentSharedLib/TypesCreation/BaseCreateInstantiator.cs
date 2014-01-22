#region

using System;
using System.Diagnostics;
using System.Reflection;

#endregion

namespace GridAgentSharedLib.TypesCreation
{
    public abstract class BaseCreateInstantiator<T> : MarshalByRefObject
    {
        private ProxyFactory _proxy;

        protected void SetProxy(ProxyFactory proxy)
        {
            _proxy = proxy;
        }

        public int GetProcessId()
        {
            return Process.GetCurrentProcess().Id;
        }

        public override object InitializeLifetimeService()
        {
            return null;
        }

        protected Assembly LoadFile(string assemblyPath)
        {
            return _proxy.LoadFile(assemblyPath);
        }

        public abstract AppDomain GetExecutingDomain();
        public abstract void Close();
    }
}