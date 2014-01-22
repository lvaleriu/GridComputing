#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GridAgentSharedLib;
using GridAgentSharedLib.Clients;
using GridComputing.Collections;
using GridComputing.Configuration;
using GridComputing.JobDistributions;
using GridComputingSharedLib;
using GridSharedLibs;

#endregion

namespace GridComputing
{
    public class TaskTestRunner<TMaster1, TSlave1, TMaster2, TSlave2> : TaskTestRunner<TMaster1, TSlave1> where TMaster1 : IMasterTask 
        where TMaster2 : IMasterTask 
        where TSlave1 : SlaveTask
        where TSlave2 : SlaveTask
    {
        public override IEnumerable<IWrapperMasterClass> GetWrapperMasters(params string[] args)
        {
            var taskElement1 = new TaskElement
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Task1",
                CustomProviderData = args == null ? null : args.First(),
                ImplementationType = GetImplementationType<TMaster1>(),
                Type = typeof(TMaster1),
                SlaveTypeName = typeof(TSlave1).FullName, // "MusicRecognition_SignatureGeneration_Slave, MusicRecognition_SignatureGeneration_Slave.SignatureGenerationSlave",
            };

            var taskElement2 = new TaskElement
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Task2",
                CustomProviderData = args == null ? null : (args.Count() >= 2 ? args.ElementAt(1) : null),
                ImplementationType = GetImplementationType<TMaster2>(),
                Type = typeof(TMaster2),
                SlaveTypeName = typeof(TSlave2).FullName, // "MusicRecognition_SignatureGeneration_Slave, MusicRecognition_SignatureGeneration_Slave.SignatureGenerationSlave",
            };

            var master1 = taskElement1.Build();
            master1.ExecutionDirectoryPath = AppDomain.CurrentDomain.BaseDirectory;

            var master2 = taskElement2.Build();
            master2.ExecutionDirectoryPath = AppDomain.CurrentDomain.BaseDirectory;


            return new List<IWrapperMasterClass> {new WrapperMasterClass(master1), new WrapperMasterClass(master2)};
        }
    }

    /// <summary>
    /// TODO Move this class to GridSharedLibs library ...
    /// </summary>
    /// <typeparam name="TMaster"></typeparam>
    /// <typeparam name="TSlave"></typeparam>
    public class TaskTestRunner<TMaster, TSlave> where TMaster : IMasterTask where TSlave : SlaveTask
    {
        private readonly int _nbAgents;

        public TaskTestRunner() : this(1)
        {

        }

        public TaskTestRunner(int nbAgents)
        {
            _nbAgents = nbAgents;
        }

        protected ImplementationType GetImplementationType<T>()
        {
            return typeof (T).GetInterfaces().Contains(typeof (IDistribImplementation)) ? ImplementationType.Distribution : ImplementationType.Free;
        }

        public virtual IEnumerable<IWrapperMasterClass> GetWrapperMasters(params string[] args)
        {
            var taskElement = new TaskElement
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Task",
                CustomProviderData = args == null ? null : args.First(),
                ImplementationType = GetImplementationType<TMaster>(),
                Type = typeof(TMaster),
                SlaveTypeName = typeof(TSlave).FullName, // "MusicRecognition_SignatureGeneration_Slave, MusicRecognition_SignatureGeneration_Slave.SignatureGenerationSlave",
            };

            var master = taskElement.Build();
            master.ExecutionDirectoryPath = AppDomain.CurrentDomain.BaseDirectory;

            return new List<IWrapperMasterClass> { new WrapperMasterClass(master) };
        }

        public void Run(params string[] customData)
        {
            //TODO Add task error tracing (init/evaluating job divison results/saving results/etc.)

            var masterWrappers = GetWrapperMasters(customData);
            var dictTasks= masterWrappers.ToDictionary(@class => @class.Id, @class => @class);

            var createInstantiator = new SlaveCreateInstantiator<TSlave>();

            FifoJobDistribution jobDistribution = new FifoJobDistribution(dictTasks, new object(), 
                new ExpiringDictionary<IAgent, short>(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1)),
                new List<IAgent>());

            var tasks = new List<Task>();

            for (int i = 0; i < _nbAgents; i++)
            {
                var task = Task.Factory.StartNew(s =>
                {
                    var agentIndex = (int) s;
                    var me = new Agent(new Client {Id = Guid.NewGuid(), UserName = "Test.Console_" + agentIndex, MachineName = Environment.MachineName + (_nbAgents == 1 ? null : "_" + agentIndex)});

                    while (true)
                    {
                        IMasterTask masterTask;
                        var taskDescriptor = jobDistribution.GetDescriptor(me, out masterTask);

                        if (masterTask == null)
                            break;

                        if (taskDescriptor.Job != null && taskDescriptor.Enabled)
                        {
                            var slave = (WrapperSlaveClass)createInstantiator.CreateSlaveTask(taskDescriptor.TypeName, typeof(TSlave).Assembly.Location);
                            ExecuteJob(taskDescriptor, masterTask, slave, me, agentIndex);
                        }
                        else
                        {
                            Thread.Sleep(100);
                        }
                    }
                }, i);

                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());
            Console.WriteLine("Done");
        }

        private void ExecuteJob(TaskDescriptor descriptor, IMasterTask master, WrapperSlaveClass slave, Agent me, int agentIndex)
        {
            Job job = descriptor.Job;

            Console.WriteLine(agentIndex + " executeJob : " + job.Id);

            slave.Initialise(descriptor);
            slave.RunInternal();

            var taskResult = new TaskResult
            {
                JobId = job.Id,
                Result = slave.Result,
                TaskId = descriptor.Id,
                TaskName = job.TaskName
            };
            master.Join(me, taskResult);
            Console.WriteLine(agentIndex + " joined results for : " + job.Id);
        }
    }
}