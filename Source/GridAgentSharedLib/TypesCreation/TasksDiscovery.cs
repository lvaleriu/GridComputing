using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Policy;

namespace GridAgentSharedLib.TypesCreation
{
    public class TasksDiscovery<T> : MarshalByRefObject
    {
        private readonly ProxyFactory _proxy;
        private AppDomain _domain;

        public TasksDiscovery(string domainName = null, string dllLocation = null)
        {
            _proxy = InitProxy(domainName, dllLocation);
        }

        public override object InitializeLifetimeService()
        {
            return null;
        }

        public IEnumerable<GridTaskType> GetGridTasks(string dllFile)
        {
            return _proxy.GetGridTasks<T>(dllFile);
        }

        private Assembly LoadFile(string assemblyPath)
        {
            return _proxy.LoadFile(assemblyPath);
        }

        private Assembly DomainOnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            Console.WriteLine("Resolve: " + args.Name);

            return LoadFile(args.Name);
        }

        /// <summary>
        ///     AppDomain's cannot probe for dll's outside of their initial folder. They can probe in the GAC, and in the
        ///     PrivateBinPath deeper into the folder, but they cannot probe into other folders.
        ///     Private assemblies are deployed in the same directory structure as the application.If the directories specified for
        ///     PrivateBinPath are not under ApplicationBase, they are ignored.
        /// </summary>
        /// <param name="domainName"></param>
        /// <param name="dllLocation"></param>
        /// <returns></returns>
        private ProxyFactory InitProxy(string domainName, string dllLocation)
        {
            var domaininfo = new AppDomainSetup
            {
                ApplicationBase = dllLocation ?? Environment.CurrentDirectory,
                //ApplicationBase = Environment.CurrentDirectory,
                //PrivateBinPath = "Temp;",
                //ApplicationName = "app name" + domainName
                //PrivateBinPathProbe = ""
            };
            Evidence adevidence = AppDomain.CurrentDomain.Evidence;

            _domain = AppDomain.CreateDomain(domainName, adevidence, domaininfo, AppDomain.CurrentDomain.PermissionSet);
            _domain.AssemblyResolve += DomainOnAssemblyResolve;
            _domain.AssemblyLoad += DomainOnAssemblyLoad;

            Type proxyType = typeof (ProxyFactory);

            //return (MasterProxyFactory)_domain.CreateInstanceFromAndUnwrap(proxyType.Assembly.FullName, proxyType.FullName);
            return (ProxyFactory) _domain.CreateInstanceAndUnwrap(proxyType.Assembly.FullName, proxyType.FullName);
        }

        private void DomainOnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            //Console.WriteLine("Load " + args.LoadedAssembly.GetName());
        }

        public void Close()
        {
            _domain.AssemblyResolve -= DomainOnAssemblyResolve;
            _domain.AssemblyLoad -= DomainOnAssemblyLoad;

            /*
             * When a thread calls Unload, the target domain is marked for unloading. The dedicated thread attempts to unload the domain, 
             * and all threads in the domain are aborted. If a thread does not abort, for example because it is executing unmanaged code, 
             * or because it is executing a finally block, then after a period of time a CannotUnloadAppDomainException is thrown in the 
             * thread that originally called Unload. If the thread that could not be aborted eventually ends, the target domain is not unloaded. 
             * Thus, in the .NET Framework version 2.0 domain is not guaranteed to unload, because it might not be possible to terminate executing threads.
             */

            AppDomain.Unload(_domain);
        }
    }
}