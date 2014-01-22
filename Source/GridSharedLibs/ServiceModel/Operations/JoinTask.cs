#region

using GridAgentSharedLib;
using GridAgentSharedLib.Clients;

#endregion

namespace GridSharedLibs.ServiceModel.Operations
{
    public class JoinTask
    {
        public Agent Agent { get; set; }
        public TaskResult Result { get; set; }
    }
}