#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Permissions;
using GridAgentSharedLib;
using GridComputing;
using GridComputingSharedLib;
using GridComputingSharedLib.TypesCreation;
using GridSharedLibs;
using NUnit.Framework;
using PrimeFinder_Master;
using PrimeFinder_Slave;

#endregion

namespace UnitTests
{
    [TestFixture]
    public class UnitTest1
    {
        [SecurityPermission(SecurityAction.Demand)]
        public static void Main()
        {
            TestSlaveTask();

            //TestMasterTask("test1");
            //TestMasterTask("test2");

            Console.ReadLine();
        }

        private static void CreateInstanceAndUnwrap()
        {
            AppDomain ad = AppDomain.CreateDomain("Test", null, new AppDomainSetup {ApplicationBase = @"C:\Git\GridComputing\release\tasks\Temp\Master"});

            try
            {
                object obj = ad.CreateInstanceAndUnwrap(
                    "RadioReco_IntervalDivide_Master, version=1.0.0.0, culture=neutral, publicKeyToken=null",
                    "RadioReco_IntervalDivide_Master.FlowRecoMaster");

                Console.WriteLine(obj);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            AppDomain.Unload(ad);
        }

        private static void TestAssemblyReflectionManager()
        {
            const string assemblyPath = @"C:\Git\GridComputing\release\tasks\MusicSigGeneration\Slave\lame_enc.dll'"; // your assembly path here
            var manager = new AssemblyReflectionManager();

            var success = manager.LoadAssembly(assemblyPath, "demodomain");
            if (!success)
                return;

            var results = manager.Reflect(assemblyPath, a =>
            {
                var names = new List<string>();
                var types = a.GetTypes();

                foreach (var t in types)
                    names.Add(t.Name);

                return names;
            });

            foreach (var name in results)
                Console.WriteLine(name);

            manager.UnloadAssembly(assemblyPath);


            //NewAppDomain.Execute(() =>
            //{
            //    new SharedClass().Print();
            //    new SharedClass().Print();
            //    new SharedClass().Print();
            //    new SharedClass().Print();
            //});
        }

        private static void AddTaskLibsAndExecuteTask()
        {
            var gridManager = new GridManager(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(10));
            gridManager.AddTaskLibs("Temp", @"C:\Git\GridComputing\release\tasks");
            gridManager.ExecuteTask("IDMASTER2", "IDSLAVE1", "test");
        }

        private static void TestSlaveTask()
        {
            var remoteServer = new RemoteServerConnector("test", useIpcChannel: true, isClient: true, enableMasterCreatorsPing:false);
            var task = remoteServer.CreateSlaveTask("TestSlave.PrimeFinderSlave", @"C:\Git\GridComputing\release\tasks\TestTask\Slave\TestSlave.dll");

            var descriptor = new TaskDescriptor {Enabled = true, Id = Guid.NewGuid(), Job = new Job(1) {Start = 10, End = 1000, TaskName = "testtask"}};
            var wrapper = new WrapperSlaveClass(task);

            wrapper.Initialise(descriptor);
            wrapper.RunInternal();

            Console.WriteLine(task.Result);

            Console.ReadLine();
        }

        private static void CreateFullMasterTask(string name)
        {
            var remoteServer = new RemoteServerConnector(name, useIpcChannel: true, isClient: false, enableMasterCreatorsPing:false);
            var task = remoteServer.CreateFullMasterTask("TestMaster.PrimeFinderDistribMaster", @"C:\Git\GridComputing\release\tasks\TestTask\Master\TestMaster.dll");
            task.Name = "test";
            remoteServer.RemoteServerClosed += instance =>
            {
                Console.WriteLine("Closing");
            };
            Console.WriteLine(task.Name);
        }

        private static void CreateMasterTask()
        {
            string masterPath = Path.Combine(@"C:\Git\GridComputing\release\tasks", "test", "Master");

            var appDomain = new MasterAppDomainCreateInstantiator<MasterTask>("new domain", masterPath);
            var instance = appDomain.CreateMasterTask("Tns.NumericGraphicSize.Master.NumericGraphicSizeMaster", @"C:\Git\GridComputing\release\tasks\test\Master\Tns.NumericGraphicSize.Master.dll");

            Console.WriteLine(instance);
        }

        [Test]
        public static void TaskTestRunnerTest()
        {
            var runner = new TaskTestRunner<PrimeFinderDistribMaster, PrimeFinderSlave>(3);
            runner.Run(null);
        }

        [Test]
        public void TestMethod1()
        {
            const string dllPath = @"x64\bass.dll";

            MachineType machineType;
            bool isUnManaged = LibTools.IsUnmanaged(dllPath, out machineType);
            Assert.IsTrue(isUnManaged);
        }

        [Test]
        public void TestMethod2()
        {
            const string dllPath = @"x64\bass.dll";
            bool isUnManaged = LibTools.UnmanagedDllIs64Bit(dllPath).Value;
            Assert.IsTrue(isUnManaged);
        }

        [Test]
        public void TestMethod3()
        {
            const string dllPath = @"x86\bass.dll";
            MachineType machineType;
            bool isUnManaged = LibTools.IsUnmanaged(dllPath, out machineType);
            Assert.IsFalse(isUnManaged);
        }

        [Test]
        public void TestMethod33()
        {
            const string dllPath = @"x86\bass.dll";

            var res = CorFlagsReader.ReadAssemblyMetadata(dllPath);

            Assert.IsTrue(res.ProcessorArchitecture == ProcessorArchitecture.X86);
        }

        [Test]
        public void TestMethod4()
        {
            const string dllPath = @"x86\bass.dll";
            bool isUnManaged = LibTools.UnmanagedDllIs64Bit(dllPath).Value;
            Assert.IsFalse(isUnManaged);
        }

        [Test]
        public void TestMethod5()
        {
            const string dllPath = @"x86\bass.dll";
            Assert.IsTrue(LibTools.Is32Bit(dllPath));
        }

        [Test]
        public void TestMethod6()
        {
            const string dllPath = @"x64\bass.dll";
            Assert.IsFalse(LibTools.Is32Bit(dllPath));
        }
    }
}