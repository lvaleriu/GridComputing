#region

using System;
using GridAgentSharedLib;
using GridAgentSharedLib.Clients;
using GridSharedLibs;
using GridSharedLibs.ServiceModel.Operations;
using GridSharedLibs.ServiceModel.Types;
using ServiceStack.Logging;
using ServiceStack.ServiceClient.Web;

#endregion

namespace GridAgent
{
    public class ServiceClient : IGlobalService
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof (ServiceClient));

        private static ServiceClientBase _serviceClient;

        public ServiceClient(string url)
        {
            _serviceClient = new JsonServiceClient(url) {Timeout = TimeSpan.FromSeconds(10)};
        }

        public TaskDescriptor StartNewJob(IAgent agent)
        {
            var response = _serviceClient.Send<StartNewJobResponse>(new StartNewJob {Agent = (Agent) agent});
            return response.Descriptor;
        }

        public FilesResponse GetFiles(string taskName)
        {
            var response = _serviceClient.Get<FilesResponse>("files/" + taskName);
            return response;
        }

        public int CancelJob(IAgent agent, TaskInformation taskInfo)
        {
            try
            {
                _serviceClient.Send<GeneralResponse>(new CancelJob {Agent = (Agent) agent, Info = taskInfo});
            }
            catch (Exception ex)
            {
                Log.Error("Unable to send cancel job order request", ex);
            }

            return 0;
        }

        public int JoinTask(IAgent agent, TaskResult result)
        {
            try
            {
                var response = _serviceClient.Send<JoinTaskResponse>(new JoinTask {Agent = (Agent) agent, Result = result});
                return response.Res;
            }
            catch (Exception ex)
            {
                Log.Error("JoinTask", ex);
                return -1;
            }
        }

        public int Disconnect(IAgent agent)
        {
            var response = _serviceClient.Send<DisconnectResponse>(new Disconnect {Agent = (Agent) agent});
            return response.Res;
        }

        public Guid Register(IAgent agent)
        {
            var response = _serviceClient.Send(new RegisterAgent {Agent = (Agent) agent});
            return response.Res;
        }

        public int UpdateJobProgress(IAgent agent, TaskProgress taskProgress)
        {
            var response = _serviceClient.Send<UpdateJobProgressResponse>(new UpdateJobProgress {Agent = (Agent) agent, TaskProgress = taskProgress});
            return response.Res;
        }

        public int Ping(IAgent agent)
        {
            _serviceClient.Send(new Ping {Agent = (Agent) agent});
            return 0;
        }

        public byte[] Download100KB()
        {
            var response = _serviceClient.Send<Download100KBResponse>(new Download100KB());
            return response.Res;
        }

        public bool LockEnter(Guid clientId, string typeName, string lockName)
        {
            var response = _serviceClient.Send<LockEnterResponse>(new LockEnter {ClientId = clientId, TypeName = typeName, LockName = lockName});
            return response.Res;
        }

        public int LockExit(Guid clientId, string typeName, string lockName)
        {
            var response = _serviceClient.Send<LockExitResponse>(new LockExit {ClientId = clientId, TypeName = typeName, LockName = lockName});
            return response.Res;
        }

        public int LockUpdate(Guid clientId, string typeName, string lockName)
        {
            var response = _serviceClient.Send<LockUpdateResponse>(new LockUpdate {ClientId = clientId, TypeName = typeName, LockName = lockName});
            return response.Res;
        }

        public GetGridTasksResponse GetGridTasks(string taskName)
        {
            return _serviceClient.Send<GetGridTasksResponse>(new GetGridTaks {TaskName = taskName});
        }
    }
}