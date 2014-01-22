#region

using System;
using System.IO;
using System.Runtime.Remoting;
using System.ServiceModel.Activation;
using GridAgentSharedLib.Clients;
using GridComputing;
using GridComputingSharedLib;
using GridSharedLibs;
using GridSharedLibs.ServiceModel.Operations;
using Newtonsoft.Json;
using ServiceStack.ServiceInterface.ServiceModel;

#endregion

namespace GridComputingServices.ClientServices
{
    /// <summary>
    ///     Default implementation for the <see cref="IGridManagementService" />.
    /// </summary>
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class GridManagementService : IGridManagementService
    {
        #region Fields

        private static readonly IGridLog Log = GridLogManager.GetLogger(typeof (GridManagementService));

        private readonly Config _config;
        private readonly GridManager _gridManager;
        private readonly object _lockAdding = new object();

        #endregion

        #region IGridManagementService Members

        public GridSummary GetGridSummary(Client client)
        {
            try
            {
                GridSummary gridSummary = _gridManager.GetGridSummary();
                return gridSummary;
            }
            catch (RemotingException)
            {
                return new GridSummary();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                throw;
            }
        }

        public GetGridTasksResponse GetGridTasks(string name)
        {
            try
            {
                return new GetGridTasksResponse {Tasks = _gridManager.GetGridTasks(name)};
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                throw;
            }
        }

        public GeneralResponse RemoveTaskRepository(string name)
        {
            try
            {
                _gridManager.RemoveTaskRepository(name, _config.TasksRepository);

                return new GeneralResponse();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return new GeneralResponse {ResponseStatus = new ResponseStatus("400", ex.Message)};
            }
        }

        public void ScheduleTask(string repositoryName, string masterId, string slaveId, string cronExpression, string customData)
        {
            _gridManager.ScheduleTask(repositoryName, masterId, slaveId, cronExpression, customData);
        }

        public AddTaskLibrariesResponse LaunchTask(string masterId, string slaveId, string customData)
        {
            var response = new AddTaskLibrariesResponse();
            try
            {
                var summary = _gridManager.LaunchTask(masterId, slaveId, customData);
                response.TaskSummary = summary;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                response.ResponseStatus = new ResponseStatus("400", ex.Message);
            }
            return response;
        }

        public void InitTaskRepository()
        {
            lock (_lockAdding)
            {
                _gridManager.RepositoryPath = _config.TasksRepository;
                foreach (var taskDir in Directory.EnumerateDirectories(_config.TasksRepository))
                {
                    try
                    {
                        _gridManager.AddTaskLibs(new DirectoryInfo(taskDir).Name, _config.TasksRepository);
                    }
                    catch (Exception ex)
                    {
                        string message = string.Format("Init task repository {0} failed", new DirectoryInfo(taskDir).Name);
                        Log.Error(message, ex);
                    }
                }
            }
        }

        public GeneralResponse AbortTask(string masterTask)
        {
            var response = new GeneralResponse();

            try
            {
                _gridManager.Abort(masterTask);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                response.ResponseStatus = new ResponseStatus("400", ex.Message);
            }

            return response;
        }

        public AddTaskLibrariesResponse AddTaskLibraries(string name, string zipFilePath)
        {
            var response = new AddTaskLibrariesResponse();

            lock (_lockAdding)
            {
                string taskTempDirPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(taskTempDirPath);

                try
                {
                    if (name.Contains(" "))
                        throw new Exception("Repository name contains illegal character(s)");

                    CompressionUtils.DecompressToDirectory(zipFilePath, taskTempDirPath, null);

                    if (!Directory.Exists(Path.Combine(taskTempDirPath, "Master")))
                        throw new DirectoryNotFoundException("Couldnt find the Master directory in the zip root level");

                    if (!Directory.Exists(Path.Combine(taskTempDirPath, "Slave")))
                        throw new DirectoryNotFoundException("Couldnt find the Slave directory in the zip root level");

                    var taskDirPath = Path.Combine(_config.TasksRepository, name);
                    LibTools.CopyAll(new DirectoryInfo(taskTempDirPath), new DirectoryInfo(taskDirPath));

                    TaskSummary summary = _gridManager.AddTaskLibs(name, _config.TasksRepository);
                    response.TaskSummary = summary;
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message, ex);
                    response.ResponseStatus = new ResponseStatus("400", ex.Message);
                }
                finally
                {
                    try
                    {
                        Directory.Delete(taskTempDirPath, true);
                    }
                    catch
                    {
                    }
                }
            }

            return response;
        }

        public ExecuteTaskResponse ExecuteTask(string masterId, string slaveId, string customData)
        {
            try
            {
                return new ExecuteTaskResponse {JsonResponse = _gridManager.ExecuteTask(masterId, slaveId, customData)};
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);

                return new ExecuteTaskResponse {ResponseStatus = new ResponseStatus("400", JsonConvert.SerializeObject(ex))};
            }
        }

        #endregion

        public GridManagementService(Config config, GridManager gridManager)
        {
            _config = config;
            _gridManager = gridManager;
        }
    }
}