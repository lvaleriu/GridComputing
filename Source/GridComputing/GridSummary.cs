#region

using System.Collections.Generic;
using GridAgentSharedLib.Clients;
using GridComputing.JobDistributions;
using GridSharedLibs;

#endregion

namespace GridComputing
{
    /// <summary>
    ///     Represents the current state of the Grid,
    ///     including running tasks and <see cref="Agent" />s.
    /// </summary>
    public class GridSummary
    {
        public GridSummary()
        {
            Statistics = new Dictionary<string, List<LightTaskExecutionStatistics>>();
            WorkingAgents = new List<Agent>();
            MessagesPerTask = new Dictionary<string, List<TaskMessage>>();
            ConnectedAgents = new List<Agent>();
            TaskSummaries = new List<TaskSummary>();
        }

        /// <summary>
        ///     Gets or sets the task summary list for the Grid.
        /// </summary>
        /// <value>The task summaries.</value>
        public List<TaskSummary> TaskSummaries { get; private set; }

        public Dictionary<string, List<LightTaskExecutionStatistics>> Statistics { get; set; }
        public List<Agent> WorkingAgents { get; private set; }
        public List<Agent> ConnectedAgents { get; private set; }
        public Dictionary<string, List<TaskMessage>> MessagesPerTask { get; private set; }

        public GridDistributionStatistics DistributionStats { get; set; }
    }
}