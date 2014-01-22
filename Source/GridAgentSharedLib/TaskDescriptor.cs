using System;
using System.Runtime.Serialization;
using GridAgentSharedLib.Clients;

namespace GridAgentSharedLib
{
    /// <summary>
    /// Allows an <see cref="Agent"/> to construct
    /// a slave task to be executed by it.
    /// This is passed from <see cref="MasterTask"/>
    /// to a slave task via the <see cref="GridManager"/>.
    /// TODO Do some properties name refactoring
    /// </summary>
    [DataContract]
    [Serializable]
    public class TaskDescriptor
    {
        private bool _enabled = true;

        /// <summary>
        /// SlaveTypeName.
        /// Gets or sets the name of the type 
        /// to be instanciated by the <see cref="Agent"/>.
        /// </summary>
        /// <value>The name of the type.</value>
        [DataMember]
        public string TypeName { get; set; }

        [DataMember]
        public string TypeAssemblyName { get; set; }

        /// <summary>
        /// Gets or sets the id of the master task 
        /// </summary>
        /// <value>The id of the task.</value>
        [DataMember]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the job information
        /// valid for a single run of the slave task.
        /// </summary>
        /// <value>The job.</value>
        [DataMember]
        public Job Job { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether 
        /// this <see cref="TaskDescriptor"/> is enabled.
        /// If not enabled, this task will not be ignored 
        /// by the <see cref="Agent"/>.
        /// This is normally used when a <see cref="MasterTask"/>
        /// has completed, but is still being polled by a slave.
        /// </summary>
        /// <value><c>true</c> if enabled; otherwise, <c>false</c>.</value>
        [DataMember]
        public bool Enabled
        {
            get { return _enabled; }
            set { _enabled = value; }
        }
    }
}