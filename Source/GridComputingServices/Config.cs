#region

using ServiceStack.Configuration;

#endregion

namespace GridComputingServices
{
    public class Config
    {
        public Config(AppSettings resourceManager)
        {
            ListeningUrl = resourceManager.GetString("ListeningUrl");
            TasksRepository = resourceManager.GetString("TasksRepository");

            ExpirationPeriodSeconds = resourceManager.Get("ExpirationPeriodSeconds", 0);
            ExpirationCheckingIntervalSeconds = resourceManager.Get("ExpirationCheckingIntervalSeconds", 0);

            EnableTrace = resourceManager.Get("EnableTrace", true);
            EnableMasterCreatorsPing = resourceManager.Get("EnableMasterCreatorsPing", true);
            CheckCancelAbuse = resourceManager.Get("CheckCancelAbuse", true);
            UseIpcChannel = resourceManager.Get("UseIpcChannel", true);
        }

        public string ListeningUrl { get; private set; }
        public string TasksRepository { get; private set; }

        public bool EnableTrace { get; private set; }
        public bool EnableMasterCreatorsPing { get; private set; }
        public bool CheckCancelAbuse { get; private set; }
        public bool UseIpcChannel { get; set; }

        public int ExpirationPeriodSeconds { get; private set; }
        public int ExpirationCheckingIntervalSeconds { get; private set; }
    }
}