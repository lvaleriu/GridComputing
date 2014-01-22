#region

using System.Collections.Generic;
using System.Threading;
using GridAgentSharedLib;
using Newtonsoft.Json;
using ServiceStack.Logging;

#endregion

namespace PrimeFinder_Slave
{
    /// <summary>
    ///     The client-side implementation of the Prime Finder task.
    ///     This task searches specified ranges for prime numbers
    ///     and then returns the results to the master.
    /// </summary>
    [TaskId("Id1")]
    public class PrimeFinderSlave : SlaveTask
    {
        private static readonly ILog log = LogManager.GetLogger(typeof (PrimeFinderSlave));
        private List<long> _primes;

        /// <summary>
        ///     Handles the Run event of the AddNumberTaskClient control.
        ///     This is where we do the processing for the task.
        /// </summary>
        public override string RunJob(Job job)
        {
            //var server = CommunicationServerFactory.GetCommunicationServer(0);
            //if (server != null)
            //    server.Send<object>(job);

            _primes = new List<long>();

            PrimesRange primesRange = job.CustomData == null ? null : JsonConvert.DeserializeObject<PrimesRange>(job.CustomData);

            long start = primesRange == null ? job.Start : primesRange.LowerLimit;
            long end = primesRange == null ? job.End : primesRange.UpperLimit;

            string infoMessage = string.Format("PrimeFinderSlave starting. Job Id is {0}. Range is {1} - {2}",
                Descriptor.Job.Id, start, end);
            log.Info(infoMessage);

            StepsCompleted = 0;
            StepsGoal = end - start;

            for (long i = start; i < end; i++)
            {
                if (IsPrime(i))
                {
                    _primes.Add(i);
                }
                StepsCompleted++;
                /* Sleep for a bit. */
                if (i%5000 == 0)
                {
                    int sleepTime = 200;
                    Thread.Sleep(sleepTime);
                }
            }

            return JsonConvert.SerializeObject(_primes);
        }

        private static bool IsPrime(long candidate)
        {
            for (int d = 2; d <= candidate/2; d++)
            {
                if (candidate%d == 0)
                {
                    return false;
                }
            }
            return candidate > 1;
        }
    }
}