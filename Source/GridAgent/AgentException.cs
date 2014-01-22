using System;

namespace GridAgent
{
    public class AgentException : Exception
    {
        public AgentException(string message)
            : base(message)
        {
            /* Intentionally left blank. */
        }

        public AgentException(string message, Exception innerException)
            : base(message, innerException)
        {
            /* Intentionally left blank. */
        }
    }
}