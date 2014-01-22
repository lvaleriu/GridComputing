#region

using System;

#endregion

namespace GridAgentSharedLib
{
    /// <summary>
    ///     The client-side task component. Carries out processing of a task under the direction of a server-side
    ///     <code>MasterTask</code> .
    /// </summary>
    public abstract class SlaveTask : MarshalByRefObject, ISlaveTask
    {
        protected TaskDescriptor Descriptor { get; private set; }

        /// <summary>
        ///     Gets or sets the progress of the task. <seealso cref="StepsGoal" />
        /// </summary>
        /// <value> The progress of the task. </value>
        public long StepsCompleted { get; protected set; }

        /// <summary>
        ///     Gets or sets the steps required to complete the task. <seealso cref="StepsCompleted" />
        /// </summary>
        /// <value> The steps required to complete the task. </value>
        public long StepsGoal { get; protected set; }

        /// <summary>
        ///     Gets or sets the result of the task. This value is valid only after the task is complete.
        /// </summary>
        /// <value> The result of processing this task. </value>
        public string Result { get; private set; }

        public string ExecutionDirectoryPath { get; set; }

        public void RunInternal()
        {
            Result = RunJob(Descriptor.Job);
        }

        public void Initialise(TaskDescriptor taskDescriptor)
        {
            Descriptor = taskDescriptor;
        }

        public abstract string RunJob(Job job);
    }
}