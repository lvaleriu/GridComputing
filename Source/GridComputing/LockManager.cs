using System;
using GridAgentSharedLib.Clients;
using GridComputing.Collections;

namespace GridComputing
{
    /// <summary>
    /// Coordinate the locking of discrete client-side code blocks
    /// by <see cref="Agent"/>s. Allows for agent safe code 
    /// execution.
    /// </summary>
    public static class LockManager
    {
        /// <summary>
        /// The collection of named code blocks and the Ids of their
        /// associated <see cref="Agent"/>s.
        /// </summary>
        private static readonly ExpiringDictionary<string, Guid> Locks
            = new ExpiringDictionary<string, Guid>(
                TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5));

        /// <summary>
        /// The collection of named code blocks 
        /// and the Ids of the <see cref="Client"/>s waiting to enter.
        /// </summary>
        private static readonly ExpiringDictionary<string, ExpiringQueue<Guid>> WaitingClients
            = new ExpiringDictionary<string, ExpiringQueue<Guid>>(
                TimeSpan.FromMinutes(10), TimeSpan.FromSeconds(60));

        /// <summary>
        /// Attempts to give exclusive access to the <see cref="Client"/>
        /// with the specified id. If another client already has exclusive
        /// access, the request will not be granted.
        /// </summary>
        /// <param name="clientId">The requesting client's id.</param>
        /// <param name="typeName">Name of the type to qualify the lock.</param>
        /// <param name="lockName">The local name of the lock.</param>
        /// <returns><code>true</code> if the client now has exclusive
        /// access to the code block; <code>false</code> otherwise.</returns>
        public static bool Enter(Guid clientId, string typeName, string lockName)
        {
            string name = typeName + lockName;
            lock (Locks.SyncLock)
            {
                Guid lockId;
                ExpiringQueue<Guid> queue;

                if (Locks.TryGetValue(name, out lockId))
                {
                    if (clientId == lockId)
                    {
                        /* Lock already held. */
                        Locks.Touch(name);
                        return true;
                    }

                    if (WaitingClients.TryGetValue(name, out queue))
                    {
                        /* Refresh the waiting caller. */
                        queue.Touch(clientId);
                    }
                    else
                    {
                        /* This is a new request, so we create a queue 
                         * for the client side monitor. */
                        queue = new ExpiringQueue<Guid>(
                            TimeSpan.FromSeconds(20),
                            TimeSpan.FromSeconds(5));
                        queue.Enqueue(clientId);
                        WaitingClients.Add(name, queue);
                    }
                    return false;
                }

                lock (WaitingClients.SyncLock)
                {
                    if (WaitingClients.TryGetValue(name, out queue))
                    {
                        Guid locksId = queue.Peek();
                        if (locksId != clientId)
                        {
                            WaitingClients.Touch(name);
                            queue.Touch(clientId);
                            return false;
                        }
                        queue.Dequeue();
                    }
                }
                Locks[name] = clientId;
            }
            return true;
        }

        /// <summary>
        /// Relinquishes exclusive access privledges 
        /// for a code block.
        /// </summary>
        /// <param name="clientId">The relinquishing client's id.</param>
        /// <param name="typeName">Name of the type to qualify the lock.</param>
        /// <param name="lockName">The local name of the lock.</param>
        public static void Exit(Guid clientId, string typeName, string lockName)
        {
            string name = typeName + lockName;
            lock (Locks.SyncLock)
            {
                Guid locksId;
                if (Locks.TryGetValue(name, out locksId) && locksId != clientId)
                {
                    return;
                }
                Locks.Remove(name);
            }
        }

        /// <summary>
        /// Lets the manager know that the client is still alive.
        /// If this is not called periodically, then any waiting 
        /// requests for exclusive locks may be expired.
        /// </summary>
        /// <param name="clientId">The calling client's id.</param>
        /// <param name="typeName">Name of the type to qualify the lock.</param>
        /// <param name="lockName">The local name of the lock.</param>
        public static void Update(Guid clientId, string typeName, string lockName)
        {
            string name = clientId + typeName + lockName;
            lock (Locks.SyncLock)
            {
                Guid locksId;
                if (Locks.TryGetValue(name, out locksId) && locksId != clientId)
                {
                    /* The lock was lost from a Timeout. */
                    lock (WaitingClients.SyncLock)
                    {
                        ExpiringQueue<Guid> queue;
                        if (WaitingClients.TryGetValue(name, out queue))
                        {
                            queue.Touch(clientId);
                            WaitingClients.Touch(name);
                        }
                        else
                        {
                            queue = new ExpiringQueue<Guid>(TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(5));
                            queue.Enqueue(clientId);
                            WaitingClients.Add(name, queue);
                        }
                    }
                    return;
                }
                Locks[name] = clientId;
            }
        }
    }
}