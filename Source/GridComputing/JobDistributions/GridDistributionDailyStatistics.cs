#region

using System;
using System.Collections.Generic;

#endregion

namespace GridComputing.JobDistributions
{
    public class GridDistributionDailyStatistics
    {
        public GridDistributionDailyStatistics()
        {
            Repositories = new List<RepCount>();
        }

        public int Count { get; set; }
        public TimeSpan Duration { get; set; }

        public List<RepCount> Repositories { get; private set; }
    }

    public class RepCount
    {
        public RepCount()
        {
            Tasks = new Dictionary<string, MasterCount>();
        }

        public string Name { get; set; }

        public int Count { get; set; }
        public TimeSpan Duration { get; set; }

        public Dictionary<string, MasterCount> Tasks { get; private set; }
    }

    public class MasterCount
    {
        public int Count { get; set; }
        public TimeSpan Duration { get; set; }
    }
}