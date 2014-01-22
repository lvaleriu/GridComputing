#region

using System;
using System.IO;
using System.Reflection;
using GridComputingSharedLib.TypesCreation;

#endregion

namespace GridSharedLibs
{
    public class MasterCreateInstantiator<T> : BaseMasterCreateInstantiator<T>
    {
        private string _taskDllPath;

        public MasterCreateInstantiator()
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

        protected override MasterProxyFactory InitProxy(string domainName, string dllLocation)
        {
            return new MasterProxyFactory();
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