namespace GridAgentSharedLib
{
    public interface ISlaveTask
    {
        /// <summary>
        ///     Gets or sets the progress of the task. <seealso cref="StepsGoal" />
        /// </summary>
        /// <value> The progress of the task. </value>
        long StepsCompleted { get; }

        /// <summary>
        ///     Gets or sets the steps required to complete the task. <seealso cref="StepsCompleted" />
        /// </summary>
        /// <value> The steps required to complete the task. </value>
        long StepsGoal { get; }

        /// <summary>
        ///     Gets or sets the result of the task. This value is valid only after the task is complete.
        /// </summary>
        /// <value> The result of processing this task. </value>
        string Result { get; }

        string ExecutionDirectoryPath { get; set; }

        void RunInternal();
        void Initialise(TaskDescriptor taskDescriptor);
    }
}