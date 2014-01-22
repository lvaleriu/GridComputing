using System.Reflection;
using GridAgentSharedLib.TypesCreation;

namespace GridComputingSharedLib.TypesCreation
{
    public class MasterProxyFactory : ProxyFactory
    {
        /// <summary>
        /// AppDomains are pure managed construct. Any unmanaged code running in the process is unaffected by the AppDomain boundaries and has full access to all process memory, data and code. 
        /// Unmanaged assemblies are not executed the same way managed assemblies are. The process of loading the assembly, and finding and executing the entry point for the unmanaged assembly 
        /// is different than the one for managed assemblies. Hence the particular failure you get. If you want to execute functions exported by an unmanaged dll, you should use P/Invoke, which
        /// will ensure that the assembly is loaded using the right mechanism and the proper entry point is invoked. You can't run code from an executable in the same process, as in your 
        /// scenario above; you can only start a new process.
        /// </summary>
        /// <param name="assemblyQualifiedName"></param>
        /// <param name="dllLocation"></param>
        /// <returns></returns>
        public IMasterTask CreateMasterTask(string assemblyQualifiedName, string dllLocation)
        {
            Assembly assembly = LoadFile(dllLocation);

            var task = (IMasterTask)CreateInstance(assemblyQualifiedName, assembly);

            var wrapperClass = new WrapperMasterClass(task);

            return wrapperClass;
        }

        public IFullMasterTask CreateFullMasterTask(string assemblyQualifiedName, string dllLocation)
        {
            Assembly assembly = LoadFile(dllLocation);

            var task = (IFullMasterTask)CreateInstance(assemblyQualifiedName, assembly);

            return task;
        }
    }
}