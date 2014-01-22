using System;

namespace GridSharedLibs
{
    public class TaskInformation
    {
        public Guid TaskId { get; set; }
        public long JobId { get; set; }
        public string TaskName { get; set; }
    }
}