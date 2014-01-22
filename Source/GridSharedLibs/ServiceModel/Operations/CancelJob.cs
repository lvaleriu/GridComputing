#region

using GridAgentSharedLib;
using GridAgentSharedLib.Clients;

#endregion

namespace GridSharedLibs.ServiceModel.Operations
{
    public class CancelJob
    {
        public Agent Agent { get; set; }
        public TaskInformation Info { get; set; }
    }

    public class AbortJob
    {
        
    }
}