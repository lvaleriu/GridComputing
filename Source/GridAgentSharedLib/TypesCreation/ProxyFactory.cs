#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

#endregion

namespace GridAgentSharedLib.TypesCreation
{
    public class ProxyFactory : MarshalByRefObject
    {
        public ProxyFactory()
        {
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += CurrentDomain_ReflectionOnlyAssemblyResolve;
//#if !DEBUG
//            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
//#endif
        }

//#if !DEBUG
//        Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
//        {
//            Console.WriteLine("CurrentDomain_AssemblyResolve: " + args.Name);
//            return Assembly.Load(args.Name);
//        }
//#endif

        Assembly CurrentDomain_ReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
        {
            Console.WriteLine("CurrentDomain_ReflectionOnlyAssemblyResolve: " + args.Name);
            return Assembly.ReflectionOnlyLoad(args.Name);
        }

        public override object InitializeLifetimeService()
        {
            return null;
        }

        public Assembly LoadFile(string assemblyPath)
        {
            try
            {
                /*
                 * Also, note that if you use LoadFrom you'll likely get a FileNotFound exception because the Assembly resolver will attempt to find 
                 * the assembly you're loading in the GAC or the current application's bin folder. Use LoadFile to load an arbitrary assembly file 
                 * instead--but note that if you do this you'll need to load any dependencies yourself.
                 * 
                 * You need to handle the AppDomain.AssemblyResolve or AppDomain.ReflectionOnlyAssemblyResolve events (depending on which load you're doing)
                 * in case the referenced assembly is not in the GAC or on the CLR's probing path. AppDomain.AssemblyResolve or AppDomain.ReflectionOnlyAssemblyResolve
                 * */
                return Assembly.LoadFile(assemblyPath);
            }
            catch (BadImageFormatException)
            {
                return null;
            }
            catch (Exception)
            {
                return null;
                // throw new InvalidOperationException(ex);
            }
        }

        protected object CreateInstance(string assemblyQualifiedName, Assembly assembly)
        {
            return assembly.CreateInstance(assemblyQualifiedName);
        }


        public IEnumerable<GridTaskType> GetGridTasks<T>(string dllFile)
        {
            var res = new List<GridTaskType>();

            try
            {
                var assembly = LoadFile(dllFile);

                if (assembly == null)
                    return res;

                foreach (Type type in assembly.GetTypes())
                {
                    if (type.IsAbstract)
                        continue;

                    if (type.IsSubclassOf(typeof (T)))
                    {
                        string id = null;

                        object[] attributes = type.GetCustomAttributes(true);
                        foreach (var attribute in attributes)
                        {
                            var idAttribute = attribute as TaskId;
                            if (idAttribute != null)
                            {
                                id = idAttribute.Id;
                            }
                        }
                        ImplementationType implementationType = type.GetInterfaces().Contains(typeof (IDistribImplementation)) 
                            ? ImplementationType.Distribution 
                            : ImplementationType.Free;

                        res.Add(new GridTaskType
                        {
                            Name = type.Name, FullName = type.FullName, Id = id,
                            ImplementationType = implementationType,
                        });
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("GetGridTasks: " + e.Message);
            }

            return res;
        }
    }
}