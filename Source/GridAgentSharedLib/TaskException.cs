#region

using System;
using Newtonsoft.Json;

#endregion

namespace GridAgentSharedLib
{
    [Serializable]
    public class TaskException : Exception
    {
        public TaskException(string message) : base(message)
        {
        }

        public TaskException(string message, Exception exception) : base(message)
        {
            JsonException = message + Environment.NewLine + (exception == null ? null : JsonConvert.SerializeObject(exception, Formatting.Indented));
        }

        public string JsonException { get; private set; }

        public override string ToString()
        {
            return Message;
        }
    }
}