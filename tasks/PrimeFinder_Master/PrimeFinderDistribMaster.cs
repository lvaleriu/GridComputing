#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GridAgentSharedLib;
using GridAgentSharedLib.Clients;
using GridComputingSharedLib;
using Newtonsoft.Json;

#endregion

namespace PrimeFinder_Master
{
    [TaskId("Id2")]
    public class PrimeFinderDistribMaster : BaseDistribMasterTask<PrimesRange>
    {
        private readonly List<long> _primeList = new List<long>();
        private readonly object _primeListLock = new object();

        protected override List<PrimesRange> StartTask(string customProviderData)
        {
            Console.WriteLine("StartTask");
            var res = new List<PrimesRange>();

            StepsGoal = 10;
            int workUnit = 10*1000;

            //Random r = new Random(1000);
            //if (r.Next() % 2 == 0)
                //throw new Exception("RandomException");

            for (int i = 0; i < StepsGoal; i++)
            {
                res.Add(new PrimesRange {LowerLimit = i*workUnit, UpperLimit = (i + 1)*workUnit});
            }

            return res;
        }

        public override void SetJob(PrimesRange jobData, IAgent agent)
        {
            base.SetJob(jobData, agent);
            jobData.Test = new[] {1, 2, 3};
        }

        protected override bool SetWorkerJobState(TaskResult taskResult, PrimesRange taskData)
        {
            //throw new Exception("SetWorkerJobState exception");
            Console.WriteLine("SetWorkerJobState");

            var array = ((IEnumerable<long>) JsonConvert.DeserializeObject<List<long>>(taskResult.Result));

            lock (_primeListLock)
                _primeList.AddRange(array);

            return true;
        }

        public override void OnSavingTaskResults()
        {
            Console.WriteLine("SavingResults");


            /* Save the results to file. */
            const string fileName = "PrimeFinderTaskOutput.txt"; // HttpContext.Current.Server.MapPath("PrimeFinderTaskOutput.txt");
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

            FireSaveTaskResult(outputText);
        }
    }
}