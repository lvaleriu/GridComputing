#region

using System;
using System.Configuration;
using GridAgentSharedLib;
using GridComputingSharedLib;
using GridComputingSharedLib.TypesCreation;
using GridSharedLibs;
using Newtonsoft.Json;

#endregion

namespace GridComputing.Configuration
{
    /// <summary>
    ///     Configuration for a <see cref="MasterTask" />.
    /// </summary>
    [Serializable]
    public class TaskElement : ConfigurationElement
    {
        public string Id { get; set; }

        /// <summary>
        ///     Gets or sets the unique name of the <see cref="TaskElement" />.
        /// </summary>
        /// <value>The name of the task.</value>
        public string Name { get; set; }

        /// <summary>
        ///     Gets or sets the type of <see cref="MasterTask" />
        ///     that will be created.
        /// </summary>
        /// <value>
        ///     The type of the <see cref="TaskElement" />.
        /// </value>
        public Type Type { get; set; }

        /// <summary>
        ///     Gets or sets the Client Task type name string of task
        ///     that will be executed by a client.
        ///     This will normally be a Silverlight assembly type name.
        ///     The type specified will be instanciated
        ///     in a clients browser (if using Silverlight)
        ///     or host application.
        ///     Be sure to specify the assembly qualified type name
        ///     including PublicKeyToken etc.
        /// </summary>
        /// <example>
        ///     GridComputing.Silverlight.AddNumberTaskClient,
        ///     GridComputing.Silverlight.AddNumberTask,
        ///     Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
        /// </example>
        /// <value>The type of the client side instance task.</value>
        public string SlaveTypeName { get; set; }
        public string SlaveTypeAssemblyName { get; set; }

        /// <summary>
        ///     Gets or sets the custom provider data.
        ///     To be used for further customization.
        /// </summary>
        /// <value>The custom provider data.</value>
        public string CustomProviderData { get; set; }

        /// <summary>
        ///     Gets or sets the priority of the <see cref="MasterTask" />,
        ///     and is used primarily by the Gridmanager to give execution
        ///     precedence to particult tasks.
        ///     A task with a higher priority will be serviced
        ///     more frequently by the <see cref="GridManager" />.
        ///     Currently unused.
        /// </summary>
        /// <value>The name of the task.</value>
        public double Priority { get; set; }

        public ICreateMasterInstance CreateInstance { get; set; }
        public InstanceCreatorType CreatorType { get; set; }
        public ImplementationType ImplementationType { get; set; }

        public string TypeName { get; set; }
        public string DllLocation { get; set; }
        public string MasterId { get; set; }

        /// <summary>
        ///     Builds the <see cref="MasterTask" /> using the <see cref="TaskElement" />
        ///     information.
        /// </summary>
        /// <returns></returns>
        public IMasterTask Build()
        {
            if (ImplementationType == ImplementationType.Free)
            {
                IMasterTask task = CreatorType == InstanceCreatorType.CurrentAppDomain
                    ? (IMasterTask) Activator.CreateInstance(Type)
                    : CreateInstance.CreateMasterTask(TypeName, DllLocation);

                var gridTaskElement = JsonConvert.DeserializeObject<GridTaskElement>(JsonConvert.SerializeObject(this));

                task.LoadInternal(gridTaskElement);

                return task;
            }
            else
            {
                IFullMasterTask task = CreatorType == InstanceCreatorType.CurrentAppDomain
                ? (IFullMasterTask)Activator.CreateInstance(Type)
                : CreateInstance.CreateFullMasterTask(TypeName, DllLocation);

                IGridLog log = null;
                if (CreatorType != InstanceCreatorType.CurrentAppDomain && CreateInstance is ILogWriter)
                {
                    log = new LogWriter((ILogWriter)CreateInstance);
                }

                var distribMasterTask = new DistribMasterTask(task, log);
                var gridTaskElement = JsonConvert.DeserializeObject<GridTaskElement>(JsonConvert.SerializeObject(this));

                distribMasterTask.LoadInternal(gridTaskElement);
                // TODO Do we really need to do this?
                task.LoadInternal(gridTaskElement);

                string execPath = task.ExecutionDirectoryPath;
                Console.WriteLine(execPath);

                return distribMasterTask;
            }
        }
    }
}