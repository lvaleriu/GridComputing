using System;
using GridAgentSharedLib;

namespace GridComputing
{
    public class TaskInfo
    {
        public TaskInfo(string masterId, Guid masterTaskId, GridTaskElement taskElement)
        {
            MasterTaskId = masterTaskId;
            TaskElement = taskElement;
            MasterId = masterId;
        }

        public string MasterId { get; set; }
        public Guid MasterTaskId { get; set; }
        public GridTaskElement TaskElement { get; set; }
    }
}