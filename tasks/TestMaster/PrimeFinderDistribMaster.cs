#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GridAgentSharedLib;
using GridComputingSharedLib;
using Newtonsoft.Json;

#endregion

namespace TestMaster
{
    [TaskId("TestId2")]
    public class PrimeFinderDistribMaster : BaseDistribMasterTask<PrimesRange>
    {
        private readonly List<long> _primeList = new List<long>();
        private readonly object _primeListLock = new object();

        protected override List<PrimesRange> StartTask(string customProviderData)
        {
            var res = new List<PrimesRange>();
       

            StepsGoal = 10;
            int workUnit = 10*100;

            for (int i = 0; i < StepsGoal; i++)
            {
                res.Add(new PrimesRange { LowerLimit = i * workUnit, UpperLimit = (i + 1) * workUnit });
            }

            //throw new Exception("Error");

            return res;
        }

        protected override bool SetWorkerJobState(TaskResult taskResult, PrimesRange taskData)
        {
            Console.WriteLine("SetWorkerJobState");
            StepsCompleted++;

            var array = ((IEnumerable<long>) JsonConvert.DeserializeObject<List<long>>(taskResult.Result));

            lock (_primeListLock)
                _primeList.AddRange(array);

            //return SetWorkerJobState(taskResult, taskData);
            return true;
        }

        public override void OnSavingTaskResults()
        {
            Console.WriteLine("SavingTaskResults");

            return;

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