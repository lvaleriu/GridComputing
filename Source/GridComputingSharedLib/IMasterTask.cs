using System;
using GridAgentSharedLib;
using GridAgentSharedLib.Clients;

namespace GridComputingSharedLib
{
    public interface IMasterTask
    {
        /// <summary>
        ///     Gets or sets the progress goal.
        ///     This is the point at which the task will be
        ///     deemed to be complete. <seealso cref="StepsCompleted" />
        /// </summary>
        /// <value>The progress goal.</value>
        long StepsGoal { get; }

        GridTaskElement TaskElement { get; }
        bool AllJobsDispatched { get; }
        int InitialisationStatus { get; }

        /// <summary>
        ///     Gets or sets a value indicating whether this <see cref="MasterTask" /> is complete.
        /// </summary>
        /// <value>
        ///     <c>true</c> if complete; otherwise, <c>false</c>.
        /// </value>
        bool Completed { get; }

        /// <summary>
        ///     Gets or sets the unique name of the task.
        /// </summary>
        /// <value>The name of this task.</value>
        string Name { get; set; }
        string ExecutionDirectoryPath { get; set; }

        string Result { get; }

        Guid Id { get; }

        /// <summary>
        ///     Gets the <code>Type</code> name
        ///     of the <see cref="Agent" /> task.
        ///     Must be fully qualified, including publickeytoken,
        ///     version etc. If it is not, client side location
        ///     of the <code>Type</code> will fail.
        /// </summary>
        /// <value>
        ///     The <code>Type</code> name
        ///     of the <see cref="Agent" /> task.
        ///     Must be fully qualified, including publickeytoken,
        ///     version etc.
        /// </value>
        string SlaveTypeName { get; }

        /// <summary>
        ///     Gets or sets the progress of the task.
        ///     This value should always be less than
        ///     the <see cref="StepsGoal" />.
        /// </summary>
        /// <value>The progress.</value>
        long StepsCompleted { get; }

        /// <summary>
        ///     Occurs when the task has finished
        ///     its work.
        /// </summary>
        event Action<object, string> Complete;

        /// <summary>
        ///     Gets the run data for the <see cref="Agent" /> slave task.
        ///     This data should encapsulate the task segment
        ///     that will be worked on by the slave. <seealso cref="Job" />
        /// </summary>
        /// <param name="agent">The agent requesting the run data.</param>
        /// <returns>The job for the agent to work on.</returns>
        Job GetJob(IAgent agent);

        /// <summary>
        ///     Joins the specified task result. This is called
        ///     when a slave task completes its <see cref="Job" />,
        ///     after having called <see cref="MasterTask.GetJob" />;
        ///     returning the results to be integrated
        ///     by the associated <see cref="MasterTask" />.
        /// </summary>
        /// <param name="agent">The agent joining the results.</param>
        /// <param name="taskResult">The task result.</param>
        void Join(IAgent agent, TaskResult taskResult);

        /// <summary>
        ///     Cancels the distributed job
        /// </summary>
        /// <param name="agent"></param>
        /// <param name="taskId"></param>
        void Cancel(IAgent agent, long taskId);

        void LostAgent(IAgent agent);
        void LoadInternal(GridTaskElement taskElement);
        void SetTraceValue(bool enable);
    }
}