#region

using System;
using System.ServiceModel;
using GridAgentSharedLib;
using GridAgentSharedLib.Clients;

#endregion

namespace GridSharedLibs.ClientServices
{
    [ServiceKnownType("GetKnownTypes", typeof (GridServiceKnownTypeCollector))]
    [ServiceContract]
    public interface IGridService
    {
        /// <summary>
        ///     Retrieves a <see cref="TaskDescriptor" />
        ///     for a task. The task is chosen by the <see cref="GridManager" />.
        ///     <seealso cref="Job" />
        /// </summary>
        /// <param name="agent">The agent.</param>
        /// <returns>
        ///     A descriptor containing
        ///     a <see cref="Job" /> to be performed.
        /// </returns>
        [OperationContract]
        TaskDescriptor StartNewJob(IAgent agent);

        [OperationContract]
        int CancelJob(IAgent agent, TaskInformation taskInfo);

        /// <summary>
        ///     Completes a <see cref="Job" />. This is called
        ///     by an <see cref="Agent" /> when it completes its
        ///     work item.
        /// </summary>
        /// <param name="agent">The agent that has completed.</param>
        /// <param name="result">The processing result.</param>
        /// <returns>A dummy int for Silverlight compatibility.</returns>
        [OperationContract]
        int JoinTask(IAgent agent, TaskResult result);

        /// <summary>
        ///     Disconnects the specified agent.
        ///     Signals that the <see cref="Agent" /> no longer wishes
        ///     to participate in Grid processing.
        /// </summary>
        /// <param name="agent">The calling agent.</param>
        /// <returns>A dummy int for Silverlight compatibility.</returns>
        [OperationContract]
        int Disconnect(IAgent agent);

        /// <summary>
        ///     Should be the first call from a prospective grid <see cref="Agent" />
        ///     Connects the specified agent to the grid by assigning it
        ///     a unique id.
        /// </summary>
        /// <param name="agent">The agent.</param>
        /// <returns>A unique id.</returns>
        [OperationContract]
        Guid Register(IAgent agent);

        /// <summary>
        ///     Notifies the current progress of the slave task.
        /// </summary>
        /// <param name="agent">The agent.</param>
        /// <param name="taskProgress">The task progress.</param>
        /// <returns>A dummy int for Silverlight compatibility.</returns>
        [OperationContract]
        int UpdateJobProgress(IAgent agent, TaskProgress taskProgress);

        /// <summary>
        ///     Notifies the server of the agent presence
        /// </summary>
        /// <param name="agent"></param>
        /// <returns></returns>
        [OperationContract]
        int Ping(IAgent agent);

        /// <summary>
        ///     Downloads approximately 100KB for measuring
        ///     client bandwidth.
        /// </summary>
        /// <returns>A dummy array of bytes.</returns>
        [OperationContract]
        byte[] Download100KB();

        #region Mutex

        /// <summary>
        ///     Attempts to give exclusive access to the <see cref="Client" />
        ///     with the specified id. If another client already has exclusive
        ///     access, the request will not be granted. <seealso cref="LockManager" />
        /// </summary>
        /// <param name="clientId">The requesting client's id.</param>
        /// <param name="typeName">Name of the type to qualify the lock.</param>
        /// <param name="lockName">The local name of the lock.</param>
        /// <returns>
        ///     <code>true</code> if the client now has exclusive
        ///     access to the code block; <code>false</code> otherwise.
        /// </returns>
        [OperationContract]
        bool LockEnter(Guid clientId, string typeName, string lockName);

        /// <summary>
        ///     Relinquishes exclusive access privledges
        ///     for a code block.
        ///     <seealso cref="LockManager" />
        /// </summary>
        /// <param name="clientId">The relinquishing client's id.</param>
        /// <param name="typeName">Name of the type to qualify the lock.</param>
        /// <param name="lockName">The local name of the lock.</param>
        /// <returns>A dummy int for Silverlight compatibility.</returns>
        [OperationContract]
        int LockExit(Guid clientId, string typeName, string lockName);

        /// <summary>
        ///     Lets the manager know that the client is still alive.
        ///     If this is not called periodically, then any waiting
        ///     requests for exclusive locks may be expired.
        ///     <seealso cref="LockManager" />
        /// </summary>
        /// <param name="clientId">The calling client's id.</param>
        /// <param name="typeName">Name of the type to qualify the lock.</param>
        /// <param name="lockName">The local name of the lock.</param>
        /// <returns>A dummy int for Silverlight compatibility.</returns>
        [OperationContract]
        int LockUpdate(Guid clientId, string typeName, string lockName);

        #endregion
    }
}