#region

using System;
using GridAgentSharedLib;
using GridAgentSharedLib.Clients;

#endregion

namespace GridAgent
{
    public interface ITaskRunner
    {
        /// <summary>
        ///     Gets the owner <see cref="Client" /> 's id.
        /// </summary>
        /// <value> The client's id. </value>
        Guid ClientId { get; }

        /// <summary>
        ///     Gets the name of the task.
        /// </summary>
        /// <value> The name of the task. </value>
        string TaskName { get; }

        /// <summary>
        ///     Gets or sets the state of the task runner. This indicates what is being done with the <see cref="SlaveTask" />
        ///     instance.
        /// </summary>
        /// <value> The state of the task runner. </value>
        TaskRunnerState State { get; }

        /// <summary>
        ///     Gets the steps completed by the <see cref="SlaveTask" /> .
        /// </summary>
        /// <value> The steps completed by the task. </value>
        long StepsCompleted { get; }

        /// <summary>
        ///     Gets the steps goal of the <see cref="SlaveTask" /> .
        /// </summary>
        /// <value> The steps goal of the task. </value>
        long StepsGoal { get; }

        /// <summary>
        ///     Gets the tasks completed by the <see cref="GridAgentSharedLib.Clients.Agent" /> .
        /// </summary>
        /// <value> The tasks completed by the agent. </value>
        int TasksCompleted { get; }

        void Start();
        event EventHandler TaskChanged;
        event EventHandler<SEventArgs> TaskError;

        /// <summary>
        ///     Occurs when the current task is complete.
        /// </summary>
        event EventHandler TaskComplete;

        void Stop();
    }
}