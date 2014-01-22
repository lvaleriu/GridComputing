#region

using Funq;
using GridSharedLibs;
using GridSharedLibs.Services;
using ServiceStack.ServiceModel.Serialization;
using ServiceStack.WebHost.Endpoints;

#endregion

namespace GridAgentTaskLauncher
{
    public class ProcessAppHost : AppHostHttpListenerBase
    {
        public ProcessAppHost()
            : base("TaskLauncherProcess", new[] {typeof (TaskLauncherServerService).Assembly})
        {
        }

        public override void Configure(Container container)
        {
            JsonDataContractSerializer.UseSerializer(new JsonNetSerializer());
        }
    }
}