#region

using Quartz;

#endregion

namespace GridComputing
{
    public class TaskJob : IJob
    {
        public void Execute(IJobExecutionContext context)
        {
            var jobInfo = (TaskJobInfo) context.JobDetail.JobDataMap["jobInfo"];

            jobInfo.GridInstance.LaunchTask(jobInfo.MasterId, jobInfo.SlaveId, jobInfo.CustomData);
        }
    }

    public class TaskJobInfo
    {
        public string RepositoryName { get; set; }
        public string MasterId { get; set; }
        public string SlaveId { get; set; }
        public string CronExpression { get; set; }
        public string CustomData { get; set; }
        public GridManager GridInstance { get; set; }
    }
}