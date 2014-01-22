#region

using System;
using System.ServiceModel.Activation;
using System.Web;
using GridAgentSharedLib;
using GridAgentSharedLib.Clients;
using GridComputing;
using GridSharedLibs;
using GridSharedLibs.ClientServices;
using ServiceStack.Logging;
using ServiceStack.Logging.Support.Logging;

#endregion

namespace GridComputingServices.ClientServices
{
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class GridService : IGridService
    {
        private static readonly byte[] Array = new byte[50000];

        private readonly GridManager _gridManager;

        public GridService(GridManager gridManager)
        {
            _gridManager = gridManager;

#if DEBUG
            LogManager.LogFactory = new DebugLogFactory();
#else
            LogManager.LogFactory = new ConsoleLogFactory();
#endif
            Log = LogManager.GetLogger(typeof (GridService));
        }

        #region IGridService Members

        /// <summary>
        ///     Retrieves a <see cref="TaskDescriptor" />
        ///     for a task. The task is chosen by the <see cref="_gridManager" />.
        ///     <seealso cref="Job" />
        /// </summary>
        /// <param name="agent">The agent.</param>
        /// <returns>
        ///     A descriptor containing
        ///     a <see cref="Job" /> to be performed.
        /// </returns>
        public TaskDescriptor StartNewJob(IAgent agent)
        {
            try
            {
                var descriptor = _gridManager.GetDescriptor(agent);
                return descriptor;
            }
            catch (Exception ex)
            {
                HandleException(ex.Message, agent, ex);
            }
            return null;
        }

        public int CancelJob(IAgent agent, TaskInformation taskInfo)
        {
            try
            {
                DecorateClientInfo(agent);
                _gridManager.Cancel(agent, taskInfo);
            }
            catch (Exception ex)
            {
                HandleException(ex.Message, agent, ex);
            }
            return 0;
        }

        /// <summary>
        ///     Completes a <see cref="Job" />. This is called
        ///     by an <see cref="Agent" /> when it completes its
        ///     work item.
        /// </summary>
        /// <param name="agent">The agent that has completed.</param>
        /// <param name="result">The processing result.</param>
        /// <returns>A dummy int for Silverlight compatibility.</returns>
        public int JoinTask(IAgent agent, TaskResult result)
        {
            try
            {
                DecorateClientInfo(agent);
                _gridManager.Join(agent, result);
            }
            catch (Exception ex)
            {
                HandleException(ex.Message, agent, ex);
            }
            return 0;
        }

        /// <summary>
        ///     Disconnects the specified agent.
        ///     Signals that the <see cref="Agent" /> no longer wishes
        ///     to participate in Grid processing.
        /// </summary>
        /// <param name="agent">The calling agent.</param>
        /// <returns>A dummy int for Silverlight compatibility.</returns>
        public int Disconnect(IAgent agent)
        {
            try
            {
                DecorateClientInfo(agent);
            }
            catch (Exception ex)
            {
                HandleException(ex.Message, agent, ex);
            }
            return 0;
        }

        /// <summary>
        ///     Should be the first call from a prospective grid <see cref="Agent" />
        ///     Connects the specified agent to the grid by assigning it
        ///     a unique id.
        /// </summary>
        /// <param name="agent">The agent.</param>
        /// <returns>A unique id.</returns>
        public Guid Register(IAgent agent)
        {
            try
            {
                DecorateClientInfo(agent);
                ((Client) agent).Id = Guid.NewGuid();
                _gridManager.Ping(agent);
            }
            catch (Exception ex)
            {
                HandleException(ex.Message, agent, ex);
            }
            return agent.Id;
        }

        /// <summary>
        ///     Notifies the current progress of the slave task.
        /// </summary>
        /// <param name="agent">The agent.</param>
        /// <param name="taskProgress">The task progress.</param>
        /// <returns>A dummy int for Silverlight compatibility.</returns>
        public int UpdateJobProgress(IAgent agent, TaskProgress taskProgress)
        {
            var concreteAgent = agent as Agent;

            try
            {
                if (concreteAgent != null)
                    concreteAgent.IPAddress = HttpContext.Current == null ? null : HttpContext.Current.Request.UserHostAddress;
                _gridManager.UpdateProgress(agent, taskProgress);
            }
            catch (Exception ex)
            {
                HandleException(ex.Message, agent, ex);
            }
            return 0;
        }

        public int Ping(IAgent agent)
        {
            var concreteAgent = agent as Agent;
            try
            {
                if (concreteAgent != null)
                    concreteAgent.IPAddress = HttpContext.Current == null ? null : HttpContext.Current.Request.UserHostAddress;
                _gridManager.Ping(agent);
            }
            catch (Exception ex)
            {
                HandleException(ex.Message, agent, ex);
            }
            return 0;
        }

        /* Results in a message size of about 100 KB. */

        /// <summary>
        ///     Downloads approximately 100KB for measuring
        ///     client bandwidth.
        /// </summary>
        /// <returns>A dummy array of bytes.</returns>
        public byte[] Download100KB()
        {
            return Array;
        }

        #endregion

        #region Private static methods

        private static void DecorateClientInfo(IClient client)
        {
            var concreteClient = client as Client;
            if (concreteClient == null)
                return;

            concreteClient.IPAddress = HttpContext.Current == null ? null : HttpContext.Current.Request.UserHostAddress;
        }

        private void HandleException(string message, Object agentIdentifier, Exception exception)
        {
            Log.Error(message + "  " + agentIdentifier, exception);
        }

        #endregion

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
        public bool LockEnter(Guid clientId, string typeName, string lockName)
        {
            try
            {
                bool result = LockManager.Enter(clientId, typeName, lockName);
                Log.Debug(string.Format("Enter return {0} clientId {1}", result, clientId));
                return result;
            }
            catch (Exception ex)
            {
                HandleException(ex.Message, clientId, ex);
            }
            return false; /* Won't get here. */
        }

        /// <summary>
        ///     Relinquishes exclusive access privledges
        ///     for a code block.
        ///     <seealso cref="LockManager" />
        /// </summary>
        /// <param name="clientId">The relinquishing client's id.</param>
        /// <param name="typeName">Name of the type to qualify the lock.</param>
        /// <param name="lockName">The local name of the lock.</param>
        /// <returns>A dummy int for Silverlight compatibility.</returns>
        public int LockExit(Guid clientId, string typeName, string lockName)
        {
            try
            {
                LockManager.Exit(clientId, typeName, lockName);
            }
            catch (Exception ex)
            {
                HandleException(ex.Message, clientId, ex);
            }
            return 0; /* Won't get here. */
        }

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
        public int LockUpdate(Guid clientId, string typeName, string lockName)
        {
            try
            {
                LockManager.Update(clientId, typeName, lockName);
            }
            catch (Exception ex)
            {
                HandleException(ex.Message, clientId, ex);
            }
            return 0; /* Won't get here. */
        }

        #endregion

        public ILog Log { get; set; }
        public static Config Config { get; set; }
    }
}