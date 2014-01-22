#region

using GridAgentSharedLib;
using GridAgentSharedLib.Clients;

#endregion

namespace GridSharedLibs.ServiceModel.Operations
{
    public class UpdateJobProgress
    {
        public Agent Agent { get; set; }
        public TaskProgress TaskProgress { get; set; }
    }
}