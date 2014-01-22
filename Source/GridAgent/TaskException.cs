using System;

namespace GridAgent
{
    public class TaskException : Exception
    {
        public TaskException()
        {
        }

        public TaskException(string message)
            : base(message)
        {
        }

        public TaskException(string message, Exception ex)
            : base(message, ex)
        {
        }
    }
}