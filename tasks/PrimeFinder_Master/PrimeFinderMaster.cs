#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using GridAgentSharedLib;
using GridAgentSharedLib.Clients;
using GridComputingSharedLib;
using Newtonsoft.Json;

#endregion

namespace PrimeFinder_Master
{
    /// <summary>
    ///     The server-side implementation of the Prime Finder task.
    ///     This task dispatches ranges of numbers to be searched
    ///     by slave agents for prime numbers.
    /// </summary>
    [TaskId("Id3")]
    public class PrimeFinderMaster : MasterTask
    {
        #region Fields

        /// <summary>
        ///     Once we reach this, we are done.
        /// </summary>
        private const long searchCeiling = 10000*10; //long.MaxValue;

        /// <summary>
        ///     This is the range size sent to agents.
        /// </summary>
        private const int rangeSize = 10000;

        /// <summary>
        ///     The results the agents send back are stored here.
        /// </summary>
        private readonly List<long> _primeList = new List<long>();

        private readonly object _primeListLock = new object();

        private readonly object _runningLock = new object();

        /// <summary>
        ///     A list of client ids that have been tasked.
        /// </summary>
        private readonly List<long> _runningTasks = new List<long>();

        private bool _allTasksDispatched;

        /// <summary>
        ///     The highest part of the search range
        ///     that we have dispatched so far.
        /// </summary>
        private long _searchUpper;

        private long _taskCounter;

        #endregion

        public PrimeFinderMaster()
        {
            StepsGoal = searchCeiling;
            Stopping += PrimeFinderTask_Stopping;
        }

        private void PrimeFinderTask_Stopping(object sender, EventArgs e)
        {
            /* Perform any cleanup activities here. */

            Trace.TraceInformation("PrimeFinderTask Stopping.");
        }

        /// <summary>
        ///     Gets the run data for the <see cref="Agent" /> slave task.
        ///     This data should encapsulate the task segment
        ///     that will be worked on by the slave. <seealso cref="Job" />
        /// </summary>
        /// <param name="agent"> The agent requesting the run data. </param>
        /// <returns> The job for the agent to work on. </returns>
        public override Job GetJob(IAgent agent)
        {
            lock (_runningLock)
            {
                if (Completed)
                {
                    throw new InvalidOperationException("Invalid state. Task is completed.");
                }

                if (_taskCounter == 0)
                    Start();

                var data = new Job(_taskCounter);

                if (!_allTasksDispatched)
                {
                    /* Determine a range to be searched. */
                    data.Start = _taskCounter*rangeSize;

                    if (data.Start < searchCeiling)
                    {
                        long proposedUpper = data.Start + rangeSize;
                        long actualUpper = proposedUpper < searchCeiling ? proposedUpper : searchCeiling;
                        data.End = actualUpper;
                        _searchUpper = data.End;
                        if (!_runningTasks.Contains(_taskCounter))
                        {
                            _runningTasks.Add(_taskCounter);
                        }
                        _taskCounter++;

                        return data;
                    }
                    if (_runningTasks.Count > 0)
                    {
                        _taskCounter = 0;
                        _allTasksDispatched = true;
                    }
                }

                if (_allTasksDispatched)
                {
                    /* Reuse previously dispatched search ranges. */
                    if (_runningTasks.Count > 0)
                    {
                        Trace.TraceInformation("Running Tasks count = " + _runningTasks.Count);
                        /* We should be using an elevator algorithm to find the next task id. 
                         * We will leave this for later. */
                        _taskCounter = _runningTasks[0];
                        data.Start = _taskCounter*rangeSize;
                        Debug.Assert(data.Start <= searchCeiling);
                        long proposedUpper = data.Start + rangeSize;
                        long actualUpper = proposedUpper < searchCeiling ? proposedUpper : searchCeiling;
                        data.End = actualUpper;

                        return data;
                    }
                }
                TriggerCompletion(true);
                throw new InvalidOperationException("Invalid state. Task is completed.");
            }
        }

        /// <summary>
        ///     Joins the specified task result. This is called
        ///     when a slave task completes its <see cref="Job" />,
        ///     after having called <see cref="GetJob" />;
        ///     returning the results to be integrated
        ///     by the associated <see cref="MasterTask" />.
        /// </summary>
        /// <param name="taskResult"> The task result. </param>
        public override void Join(IAgent agent, TaskResult taskResult)
        {
            lock (_runningLock)
            {
                if (Completed)
                {
                    return;
                }

                var array = ((IEnumerable<long>) JsonConvert.DeserializeObject<List<long>>(taskResult.Result));
                foreach (var prime in array)
                {
                    /* We should verify that it's really prime! */
                    lock (_primeListLock)
                    {
                        if (!_primeList.Contains(prime))
                        {
                            _primeList.Add(prime);
                        }
                    }
                }

                _runningTasks.Remove(taskResult.JobId);

                StepsCompleted += rangeSize;

                if (_runningTasks.Count == 0
                    && _searchUpper >= searchCeiling)
                {
                    /* Save the results to file. */
                    string fileName = "PrimeFinderTaskOutput.txt"; // HttpContext.Current.Server.MapPath("PrimeFinderTaskOutput.txt");
                    var sb = new StringBuilder();
                    lock (_primeListLock)
                    {
                        foreach (long prime in _primeList)
                        {
                            sb.Append(prime);
                            sb.Append(" ");
                        }
                    }
                    string outputText = sb.ToString();
                    File.WriteAllText(fileName, outputText);

                    Stop();

                    TriggerCompletion(true); /* We're done. */
                }
            }
        }

        protected override void OnStarting()
        {
            /* Perform tasks, before we begin, here. */

            Trace.TraceInformation("PrimeFinderTask Starting.");
        }

        protected override void OnStopping()
        {
            
        }
    }
}