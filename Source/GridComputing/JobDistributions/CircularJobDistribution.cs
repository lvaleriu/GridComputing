#region

using System;
using System.Collections.Generic;
using GridAgentSharedLib;
using GridAgentSharedLib.Clients;
using GridComputingSharedLib;
using GridSharedLibs;

#endregion

namespace GridComputing.JobDistributions
{
    public class CircularJobDistribution : IJobDistribution
    {
        public static IGridLog Log = GridLogManager.GetLogger(typeof (CircularJobDistribution));
        private static IMasterTask _lastTask;
        private static readonly object LastTaskLock = new object();

        public CircularJobDistribution(Dictionary<Guid, MasterTask> tasks, object tasksLock)
        {
            TasksLock = tasksLock;
            Tasks = tasks;
        }

        #region Implementation of IJobDistribution

        public TaskDescriptor GetDescriptor(IAgent agent, out IMasterTask masterTask)
        {
            masterTask = null;
            lock (LastTaskLock)
            {
                if (Tasks.Count == 0)
                {
                    return new TaskDescriptor {Enabled = false, Job = new Job(0)};
                }

                /* Get the next task for allocation. What we might do later 
				 * is add a priority system for the tasks, and allocate tasks 
				 * according to a running average execution time. */
                foreach (var pair in Tasks)
                {
                    if (_lastTask == pair.Value)
                    {
                        /* We are looking at the lastTask, and we want 
						 * the next one in the list, or, if the last element, the first.*/
                        _lastTask = null;
                        continue;
                    }

                    if (_lastTask == null)
                    {
                        /* Initial case, or the iteration 
						 * after the lastTask was matched. */
                        _lastTask = masterTask = pair.Value;
                        break;
                    }
                }

                if (masterTask == null)
                {
                    /* Back to the start of the list. */
                    foreach (var pair in Tasks)
                    {
                        masterTask = _lastTask = pair.Value;
                        break;
                    }
                }
            }
            if (masterTask == null)
            {
                return new TaskDescriptor {Enabled = false, Job = new Job(0)};
            }

            var descriptor = new TaskDescriptor {TypeName = masterTask.SlaveTypeName, Id = masterTask.Id,};
            if (!masterTask.Completed)
            {
                try
                {
                    descriptor.Job = masterTask.GetJob(agent);
                }
                    //TODO Move this section into the GridManager try catch section of the GetDescriptor methode
                catch (InvalidOperationException ex)
                {
                    string jobId = descriptor.Job != null ? descriptor.Job.Id.ToString() : "Job is null";
                    Log.Warn("Unable to get Job for Job Id: " + jobId, ex);
                    descriptor.Enabled = false;
                }
            }

            if (descriptor.Job == null)
            {
                return new TaskDescriptor {Enabled = false, Job = new Job(0)};
            }

            descriptor.TypeAssemblyName = masterTask.TaskElement.SlaveTypeAssemblyName;
            descriptor.Job.TaskName = masterTask.Name;

            return descriptor;
        }

        public void OnCheckCancelAbuse(IAgent agent, TaskInformation info)
        {
            throw new NotImplementedException();
        }

        public void SetComputingDistribution(Dictionary<string, Tuple<List<string>, double>> computingDistribution)
        {
            throw new NotImplementedException();
        }

        public void AddNewStatistics(string repositoryName, string masterId)
        {
            throw new NotImplementedException();
        }

        public void SetDivisonStats(IAgent agent, GridTaskElement taskElement, bool init, bool cancel = false)
        {
            throw new NotImplementedException();
        }

        public Dictionary<string, List<LightTaskExecutionStatistics>> GetTasksExecutionStatistics()
        {
            throw new NotImplementedException();
        }

        public GridDistributionStatistics GetGridStatistics()
        {
            throw new NotImplementedException();
        }

        #endregion

        private object TasksLock { get; set; }
        private Dictionary<Guid, MasterTask> Tasks { get; set; }
    }
}