#region

using System;
using System.Runtime.Serialization;
using GridAgentSharedLib;
using GridAgentSharedLib.Clients;

#endregion

namespace GridSharedLibs
{
    /// <summary>
    ///     The progress that has been made performing
    ///     a <see cref="Job" /> by an <see cref="Agent" />.
    /// </summary>
    [DataContract]
    [Serializable]
    public class TaskProgress
    {
        /// <summary>
        /// The name set by the TaskId attribute for this task or the type name of the task
        /// </summary>
        [DataMember]
        public string TaskName { get; set; }

        /// <summary>
        ///     Gets or sets the id of the task that
        ///     this instance applies.
        /// </summary>
        /// <value>The id of the task.</value>
        [DataMember]
        public Guid TaskId { get; set; }

        /// <summary>
        ///     Gets or sets the steps so far. <seealso cref="StepsGoal" />
        /// </summary>
        /// <value>The steps.</value>
        [DataMember]
        public long StepsCompleted { get; set; }

        /// <summary>
        ///     Gets or sets the steps that mush be completed
        ///     before the task is deemed complete. <seealso cref="StepsCompleted" />
        /// </summary>
        /// <value>The steps goal.</value>
        [DataMember]
        public long StepsGoal { get; set; }

        /* TODO: include some more statistical information here. */
    }
}