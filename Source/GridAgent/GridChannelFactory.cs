#region

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using GridAgentSharedLib;

#endregion

namespace GridAgent
{
    public class GridChannelFactory : IChannelFactory
    {
        private ICommunicationServerFactory _communicationServerFactory;
        private string _pluginPath;

        #region IChannelFactory Members

        public ICommunicationServerFactory GetCommunicationServerFactory(string pluginPath)
        {
            if (_communicationServerFactory != null)
                return _communicationServerFactory;

            _pluginPath = pluginPath;

            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += CurrentDomainReflectionOnlyAssemblyResolve;
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainOnAssemblyResolve;

            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var dllPath in Directory.EnumerateFiles(pluginPath, "*.dll"))
            {
                if (loadedAssemblies.Any(a => a.Location == dllPath))
                    continue;

                Type communicationServerFactory = null;
                
                try
                {
                    var dllAssembly = Assembly.ReflectionOnlyLoadFrom(dllPath);

                    //if (dllPath.Contains("LinkCommunicationServer"))
                    //    Console.WriteLine("Ok");

                    foreach (var type in dllAssembly.GetTypes())
                    {
                        var interfaces = type.FindInterfaces((t, criteria) => t.ToString() == typeof(ICommunicationServerFactory).ToString(), null);

                        if (interfaces.Length > 0)
                        {
                            communicationServerFactory = type;
                            break;
                        }
                    }

                    if (communicationServerFactory != null)
                    {
                        var assembly = Assembly.LoadFrom(dllPath);
                        communicationServerFactory = assembly.GetType(communicationServerFactory.ToString());

                        _communicationServerFactory = (ICommunicationServerFactory) Activator.CreateInstance(communicationServerFactory);

                        return _communicationServerFactory;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            return null;
        }

        private Assembly CurrentDomainOnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                string assemblyName = args.Name.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries).First();
                return Assembly.Load(Path.Combine(string.Format(@"{0}\{1}.dll", _pluginPath, assemblyName)));
            }
            catch (Exception)
            {
            }

            return null;
        }

        Assembly CurrentDomainReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                return Assembly.ReflectionOnlyLoad(args.Name);
            }
            catch (Exception)
            {
                string assemblyName = args.Name.Split(new[] {","}, StringSplitOptions.RemoveEmptyEntries).First();
                return Assembly.ReflectionOnlyLoadFrom(Path.Combine(string.Format(@"{0}\{1}.dll", _pluginPath, assemblyName)));
            }
        }

        public void CloseChannel()
        {
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve -= CurrentDomainReflectionOnlyAssemblyResolve;
            AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomainOnAssemblyResolve;

            if (_communicationServerFactory != null)
            {
                _communicationServerFactory.Close();
            }
        }

        #endregion
    }
}