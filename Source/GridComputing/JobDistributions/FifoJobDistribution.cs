#region

using System;
using System.Collections.Generic;
using System.Net.Sockets;
using GridAgentSharedLib;
using GridAgentSharedLib.Clients;
using GridComputing.Collections;
using GridComputingSharedLib;

#endregion

namespace GridComputing.JobDistributions
{
    public class FifoJobDistribution : JobDistributionBase
    {
        #region Implementation of IJobDistribution

        public FifoJobDistribution(Dictionary<Guid, IWrapperMasterClass> tasks, object tasksLock, ExpiringDictionary<IAgent, short> agentsPing, List<IAgent> workingAgents)
            : base(tasks, tasksLock, agentsPing, workingAgents)
        {
        }

        /// <summary>
        ///     NOTE : When calling this method we are in the tasksLock
        /// </summary>
        protected override bool GetNextMasterTask(ref IMasterTask masterTask, List<string> avoidTaskList, ref Guid masterGuid, Dictionary<Guid, IWrapperMasterClass> tasks)
        {
            var unaccesibleProxies = new List<Guid>();
            bool foundJob = false;

            //Check what happens when the tasks are remote proxies and they get disconnected -> Should remove the task from collection
            foreach (var pair in tasks)
            {
                IWrapperMasterClass el = pair.Value;

                try
                {
                    if (avoidTaskList.Contains(el.Name))
                        continue;

                    //if (el.Completed)
                    //{
                    //    Console.WriteLine("Task '{0}' is completed but is still in the list which contains {1} elements!", el.Name, _tasks.Count);
                    //}

                    //  Try to take first the tasks that dont have all the job dispatched
                    if (!el.Completed && !el.AllJobsDispatched)
                    {
                        masterTask = el;
                        if (el.InitialisationStatus != InitializationStatus.Initializing)
                        {
                            foundJob = true;
                            masterGuid = pair.Key;
                            break;
                        }
                    }
                }
                catch (SocketException ex)
                {
                    // Should remove the task from collection
                    unaccesibleProxies.Add(pair.Key);
                    el.FireFailedCompletion(new TaskException("GetNextMasterTask", ex).JsonException);
                }
            }

            foreach (Guid unaccesibleProxy in unaccesibleProxies)
            {
                tasks.Remove(unaccesibleProxy);
            }
            return foundJob;
        }

        #endregion
    }
}