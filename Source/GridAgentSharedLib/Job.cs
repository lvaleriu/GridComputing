using System;
using System.Runtime.Serialization;
using GridAgentSharedLib.Clients;

namespace GridAgentSharedLib
{
    /// <summary>
    /// The unit of work carried out by an <see cref="Agent"/>.
    /// </summary>
    [DataContract]
    [Serializable]
    public class Job
    {
        public Job(long taskId)
        {
            Id = taskId;
        }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [DataMember]
        public long Id { get; set; }

        /// <summary>
        /// Gets the name of the task.
        /// </summary>
        /// <value>The name of the task.</value>
        [DataMember]
        public string TaskName { get; set; }

        /// <summary>
        /// Gets or sets the start.
        /// </summary>
        /// <value>The start.</value>
        [DataMember]
        public long Start { get; set; }

        /// <summary>
        /// Gets or sets the end.
        /// </summary>
        /// <value>The end.</value>
        [DataMember]
        public long End { get; set; }

        /// <summary>
        /// Gets or sets the custom data.
        /// </summary>
        /// <value>The custom data.</value>
        [DataMember]
        public string CustomData { get; set; }
    }
}