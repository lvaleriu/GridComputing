using System.Reflection;

namespace GridAgentSharedLib.TypesCreation
{
    public class SlaveProxyFactory : ProxyFactory
    {
        public ISlaveTask CreateSlaveTask(string assemblyQualifiedName, string dllLocation)
        {
            Assembly assembly = LoadFile(dllLocation);

            var slaveTask = (ISlaveTask)CreateInstance(assemblyQualifiedName, assembly);

            var wrapperClass = new WrapperSlaveClass(slaveTask);

            return wrapperClass;
        }
    }
}