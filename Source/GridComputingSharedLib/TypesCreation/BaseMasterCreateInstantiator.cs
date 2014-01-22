using System;
using GridAgentSharedLib.TypesCreation;

namespace GridComputingSharedLib.TypesCreation
{
    public abstract class BaseMasterCreateInstantiator<T> : BaseCreateInstantiator<T>, ICreateMasterInstance
    {
        private readonly MasterProxyFactory _proxy;

        protected BaseMasterCreateInstantiator(string domainName = null, string dllLocation = null)
        {
            _proxy = InitProxy(domainName, dllLocation);
            SetProxy(_proxy);
        }

        public virtual IMasterTask CreateMasterTask(string assemblyQualifiedName, string dllLocation)
        {
            var masterTask = _proxy.CreateMasterTask(assemblyQualifiedName, dllLocation);
            masterTask.ExecutionDirectoryPath = AppDomain.CurrentDomain.BaseDirectory;
            return masterTask;
        }

        public IFullMasterTask CreateFullMasterTask(string assemblyQualifiedName, string dllLocation)
        {
            var masterTask = _proxy.CreateFullMasterTask(assemblyQualifiedName, dllLocation);
            masterTask.ExecutionDirectoryPath = AppDomain.CurrentDomain.BaseDirectory;
            return masterTask;
        }

        protected abstract MasterProxyFactory InitProxy(string domainName, string dllLocation);
    }
}