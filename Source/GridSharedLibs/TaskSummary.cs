#region

using System.Runtime.Serialization;
using GridAgentSharedLib.Clients;

#endregion

namespace GridSharedLibs
{
    /// <summary>
    ///     A summary of the current state
    ///     of a task.
    /// </summary>
    [DataContract]
    public class TaskSummary
    {
        private TaskProgress _progress = new TaskProgress();

        /// <summary>
        ///     Gets or sets the name, which is the name
        ///     of the associated <see cref="MasterTask" />.
        /// </summary>
        /// <value>The name of the task.</value>
        [DataMember]
        public string Name { get; set; }

        /// <summary>
        ///     Gets or sets the number of <see cref="Agent" />s
        ///     that are working on the the task.
        /// </summary>
        /// <value>The agent count.</value>
        [DataMember]
        public int AgentCount { get; set; }

        /// <summary>
        ///     Gets or sets the total megaFLOPS
        ///     of processor power dedicated to the task.
        /// </summary>
        /// <value>
        ///     The megaFLOPS of processing power
        ///     dedicated to the task.
        /// </value>
        [DataMember]
        public long MFlops { get; set; }

        /// <summary>
        ///     Gets or sets the bandwidth, in KiloBytes per second,
        ///     at which associated task <see cref="Agent" />s
        ///     are capable of receiving data from server.
        /// </summary>
        /// <value>The total bandwidth in KiloBytes.</value>
        [DataMember]
        public double BandwidthKBps { get; set; }

        /// <summary>
        ///     Gets or sets the progress.
        ///     The progress indicates what work has been
        ///     carried so far on the task, and what
        ///     remains to be carried out.
        /// </summary>
        /// <value>The total progress of the task.</value>
        [DataMember]
        public TaskProgress Progress
        {
            get { return _progress; }
            set { _progress = value; }
        }

        public override string ToString()
        {
            return string.Format("{0} Progress : Goal {1} / Completed {2}", Name, Progress.StepsGoal, Progress.StepsCompleted);
        }
    }
}