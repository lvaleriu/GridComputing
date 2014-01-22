using System;

namespace GridAgentSharedLib
{
    [AttributeUsage(AttributeTargets.Class)]
    public class TaskId : Attribute
    {
        public readonly string Id;

        public TaskId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new Exception("The task value is empty!");

            Id = id;
        }

        public string Info
        { get; set; }
    }
}