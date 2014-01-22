#region

using System;
using System.Collections.Generic;
using GridAgentSharedLib.Clients;

#endregion

namespace GridComputing
{
    public class TaskExecutionStatistics
    {
        public TaskExecutionStatistics()
        {
            StatsPerAgent = new Dictionary<Guid, DivisionExecutionStat>();
        }

        public string RepositoryName { get; set; }
        public string MasterId { get; set; }

        public Dictionary<Guid, DivisionExecutionStat> StatsPerAgent { get; set; }
    }

    public class LightTaskExecutionStatistics
    {
        public LightTaskExecutionStatistics()
        {
            StatsPerAgent = new List<LightDivisionExecutionStat>();
        }

        public string MasterId { get; set; }

        public List<LightDivisionExecutionStat> StatsPerAgent { get; set; }
    }

    public class DivisionExecutionStat
    {
        public DivisionExecutionStat()
        {
            Durations = new List<TimeSpan>();
            Dates = new List<ExecutionTime>();
        }

        public Agent AgentInfo { get; set; }
        public int ExecutionCount { get; set; }
        public List<TimeSpan> Durations { get; private set; }
        public List<ExecutionTime> Dates { get; private set; }

        public DateTime LastDistributionTImeUtc { get; set; }
    }

    public class ExecutionTime
    {
        public DateTime Date { get; set; }
        public TimeSpan Duration { get; set; }
    }

    public class LightDivisionExecutionStat
    {
        public LightDivisionExecutionStat()
        {
            Durations = new List<TimeSpan>();
        }

        public LightAgent AgentInfo { get; set; }
        public int ExecutionCount { get; set; }
        public List<TimeSpan> Durations { get; set; }
        public TimeSpan AverageDuration { get; set; }
    }

    public class LightAgent
    {
        public Guid Id { get; set; }
        public string IPAddress { get; set; }
        public string MachineName { get; set; }
        public string UserName { get; set; }
        public double ProcessorCount { get; set; }
        public double TotalPhysicalMemory { get; set; }
    }
}