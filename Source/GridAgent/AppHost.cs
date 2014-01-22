#region

using System;
using System.Threading;
using System.Threading.Tasks;
using GridSharedLibs;
using ServiceStack.Configuration;
using ServiceStack.Logging;
using ServiceStack.Logging.Support.Logging;
using ServiceStack.ServiceModel.Serialization;

#endregion

namespace GridAgent
{
    public class AppHost
    {
        #region Fields

        private static ILog _log;
        private Config _config;
        private TaskRunner _taskRunner;

        #endregion

        public AppHost()
        {
            LogManager.LogFactory = new ConsoleLogFactory();

            _log = LogManager.GetLogger(typeof (AppHost));
        }

        public void Init()
        {
            JsonDataContractSerializer.UseSerializer(new JsonNetSerializer());

            _config = new Config(new AppSettings());

            _log.Info(string.Format("Grid computing web service uri : {0}", _config.UrlBase));
            _log.Info(string.Format("The slave task folder is {0}", _config.SlaveTasksFolder));
        }

        public void Start()
        {
            _taskRunner = new TaskRunner(_config, new ServiceClient(_config.UrlBase));

            _taskRunner.TaskError += TaskRunnerTaskError;
            _taskRunner.TaskComplete += TaskManagerTaskComplete;
            _taskRunner.TaskChanged += TaskManagerTaskChanged;

            Task.Factory.StartNew(StartTaskRunner);
        }

        public void Stop()
        {
            if (_taskRunner != null)
            {
                _taskRunner.TaskError -= TaskRunnerTaskError;
                _taskRunner.TaskComplete -= TaskManagerTaskComplete;
                _taskRunner.TaskChanged -= TaskManagerTaskChanged;

                _taskRunner.Stop();
            }
        }

        #region Events handling

        private void TaskRunnerTaskError(object sender, SEventArgs e)
        {
            Thread.Sleep(2000);
            StartTaskRunner();
        }

        private void TaskManagerTaskComplete(object sender, EventArgs e)
        {
            UpdateTasksCompleted();
        }

        private void TaskManagerTaskChanged(object sender, EventArgs e)
        {
            string taskName = _taskRunner.TaskName;
            _log.Info(string.Format("TaskChanged : {0}", taskName));
        }

        #endregion

        #region Private methods

        private void StartTaskRunner()
        {
            try
            {
                _taskRunner.Start();
            }
            catch (Exception ex)
            {
                const string message = "Unable to start the task manager.";
                _log.Fatal(message, ex);

                Thread.Sleep(2000);
                StartTaskRunner();
            }
        }

        private void UpdateTasksCompleted()
        {
            _log.Info(string.Format("TaskComplete Completed : {0}", _taskRunner.TasksCompleted));
        }

        #endregion
    }
}