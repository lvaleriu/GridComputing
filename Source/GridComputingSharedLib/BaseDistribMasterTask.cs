#region

using System.Collections.Generic;
using System.Linq;
using GridAgentSharedLib;
using GridAgentSharedLib.Clients;
using Newtonsoft.Json;

#endregion

namespace GridComputingSharedLib
{
    public abstract class BaseDistribMasterTask<T> : MasterTask, IFullMasterTask where T : class 
    {
        protected readonly IGridLog _log = GridLogManager.GetLogger(typeof(BaseDistribMasterTask<T>));

        public bool SetWorkerJobState(TaskResult taskResult, string taskData)
        {
            if (EnableTrace)
                _log.Info("Set state for job: " + taskData);

            return SetWorkerJobState(taskResult, JsonConvert.DeserializeObject<T>(taskData));
        }

        public List<string> StartInitTask(string customProviderData)
        {
            var taskDivisions = StartTask(customProviderData);

            if (EnableTrace)
                _log.Info("There are " + taskDivisions.Count + " job divisions for this task.");

            return taskDivisions.Select(JsonConvert.SerializeObject).ToList();
        }

        public string SetJob(string jobData, IAgent agent)
        {
            if (EnableTrace)
                _log.Info(string.Format("Set job data for agent : {0} and job division : {1}", agent.MachineName, jobData));

            T data = JsonConvert.DeserializeObject<T>(jobData);
            SetJob(data, agent);

            return JsonConvert.SerializeObject(data);
        }

        public bool RedispatchInTreatmentJobs { get; set; }

        public abstract void OnSavingTaskResults();

        /// Returns the validation of the job division done by an agent
        /// </summary>
        /// <param name="taskResult"></param>
        /// <param name="taskData"></param>
        /// <returns></returns>
        protected abstract bool SetWorkerJobState(TaskResult taskResult, T taskData);

        protected abstract List<T> StartTask(string customProviderData);

        // ReSharper disable once MemberCanBeProtected.Global
        public virtual void SetJob(T jobData, IAgent agent)
        {
        }

        public override Job GetJob(IAgent agent)
        {
            throw new System.NotImplementedException();
        }

        public override void Join(IAgent agent, TaskResult taskResult)
        {
            throw new System.NotImplementedException();
        }

        protected override void OnStarting()
        {
            throw new System.NotImplementedException();
        }

        protected override void OnStopping()
        {
            throw new System.NotImplementedException();
        }
    }
}