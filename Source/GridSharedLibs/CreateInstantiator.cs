#region

using System;
using System.IO;
using System.Reflection;
using GridAgentSharedLib;
using GridAgentSharedLib.TypesCreation;

#endregion

namespace GridSharedLibs
{
    public class SlaveCreateInstantiator<T> : BaseSlaveCreateInstantiator<T>
    {
        private string _taskDllPath;

        public SlaveCreateInstantiator()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainAssemblyResolve;
        }

        public void SetDirPath(string taskDllPath)
        {
            _taskDllPath = taskDllPath;
        }

        private Assembly CurrentDomainAssemblyResolve(object sender, ResolveEventArgs args)
        {
            string path = Path.Combine(_taskDllPath, args.Name.Substring(0, args.Name.IndexOf(",")) + ".dll");
            return Assembly.LoadFile(path);
        }

        public override ISlaveTask CreateSlaveTask(string assemblyQualifiedName, string dllLocation)
        {
            _taskDllPath = new FileInfo(dllLocation).Directory.FullName;

            return base.CreateSlaveTask(assemblyQualifiedName, dllLocation);
        }

        protected override SlaveProxyFactory InitProxy(string domainName, string dllLocation)
        {
            return new SlaveProxyFactory();
        }

        public override AppDomain GetExecutingDomain()
        {
            return AppDomain.CurrentDomain;
        }

        public override void Close()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomainAssemblyResolve;
        }
    }
}