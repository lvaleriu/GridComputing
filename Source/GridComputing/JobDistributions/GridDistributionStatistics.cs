using System;
using System.Collections.Generic;

namespace GridComputing.JobDistributions
{
    public class GridDistributionStatistics
    {
        public GridDistributionStatistics()
        {
            DailyStatistics = new Dictionary<DateTime, GridDistributionDailyStatistics>();
            RepositoryStats = new List<RepositoryStat>();
        }

        public int Count { get; set; }
        public TimeSpan TotalDuration { get; set; }

        public List<RepositoryStat> RepositoryStats { get; private set; }

        public Dictionary<DateTime, GridDistributionDailyStatistics> DailyStatistics { get; private set; }
    }

    public class RepositoryStat
    {
        public RepositoryStat()
        {
            Stats = new Dictionary<string, MasterStat>();
        }

        public string Name { get; set; }
        public double TimePerc { get; set; }
        public double CountPer { get; set; }

        public Dictionary<string, MasterStat> Stats { get; private set; } 
    }

    public class MasterStat
    {
        public double TimePerc { get; set; }
        public double CountPer { get; set; }
        public double AgentsPerc { get; set; }
    }
}