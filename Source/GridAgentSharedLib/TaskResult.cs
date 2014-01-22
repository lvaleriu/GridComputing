#region

using System;
using System.Runtime.Serialization;
using GridAgentSharedLib.Clients;

#endregion

namespace GridAgentSharedLib
{
    /// <summary>
    ///     The result of a <see cref="Job" /> executed
    ///     by a <see cref="Agent" /> slave task.
    /// </summary>
    [DataContract]
    [KnownType("GetKnownTypes")]
    [Serializable]
    public class TaskResult // : IExtensibleDataObject
    {
        /// <summary>
        ///     Gets or sets the id of the assoicated <see cref="Job" />.
        /// </summary>
        /// <value>The job id.</value>
        [DataMember]
        public long JobId { get; set; }

        /// <summary>
        ///     Gets or sets the name of the associated <see cref="MasterTask" />.
        /// </summary>
        /// <value>The name of the owner task.</value>
        [DataMember]
        public string TaskName { get; set; }

        [DataMember]
        public Guid TaskId { get; set; }

        /// <summary>
        ///     Gets or sets the result of the run.
        ///     This is used by the <see cref="MasterTask" />
        ///     to join with other results from other <see cref="Job" />s.
        /// </summary>
        /// <value>The result of the run.</value>
        [DataMember]
        public string Result { get; set; }
    }
}