#region

using System.Diagnostics;
using GridSharedLibs.ServiceModel.Operations.ProcessLauncher;
using ServiceStack.ServiceInterface;

#endregion

namespace GridSharedLibs.Services
{
    public class TaskLauncherServerService : Service
    {
        public void Any(PingProcess ping)
        {
        }

        public void Any(CloseProcess ping)
        {
        }

        public int Any(GetProcessId ping)
        {
            return Process.GetCurrentProcess().Id;
        }
    }
}