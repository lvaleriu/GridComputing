using System.IO;

namespace GridAgentSharedLib.TypesCreation
{
    public abstract class BaseSlaveCreateInstantiator<T> : BaseCreateInstantiator<T>, ICreateSlaveInstance
    {
        private readonly SlaveProxyFactory _proxy;

        protected BaseSlaveCreateInstantiator(string domainName = null, string dllLocation = null)
        {
            _proxy = InitProxy(domainName, dllLocation);
            SetProxy(_proxy);
        }

        public virtual ISlaveTask CreateSlaveTask(string assemblyQualifiedName, string dllLocation)
        {
            var taskDllPath = new FileInfo(dllLocation).Directory.FullName;
            var slaveTask = _proxy.CreateSlaveTask(assemblyQualifiedName, dllLocation);
            slaveTask.ExecutionDirectoryPath = taskDllPath;
            return slaveTask;
        }

        protected abstract SlaveProxyFactory InitProxy(string domainName, string dllLocation);
    }
}