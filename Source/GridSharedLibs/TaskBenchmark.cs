using System;

namespace GridSharedLibs
{
    public class TaskBenchmark
    {
        public string RepositoryName { get; set; }
        public string TaskName { get; set; }

        public TimeSpan Duration { get; set; }
        public double CpuUsagePercentage { get; set; }
    }
}