#region

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using AutoMapper;
using GridAgentSharedLib.Clients;
using GridComputing;
using GridComputingServices.ClientServices;
using GridSharedLibs;
using GridSharedLibs.ServiceModel.Operations;
using Microsoft.Win32;
using ServiceStack.Configuration;
using ServiceStack.ServiceClient.Web;

#endregion

namespace GridManagerWpf
{
    /// <summary>
    ///     Interaction logic for GridSummary.xaml
    /// </summary>
    public partial class GridSummaryWnd : INotifyPropertyChanged
    {
        #region Fields

        private const int RefreshGridMs = 1000;
        private readonly IGridManagementService _serviceManager;
        private readonly Timer _timer;
        private ICollectionView _collectionView;
        private string _data;

        private GridTaskDataModel _selectedMaster;
        private GridTaskDataModel _selectedSlave;

        private string _taskFileZip;
        private string _taskName;

        #endregion

        public GridSummaryWnd()
        {
            InitializeComponent();

            Mapper.CreateMap<GridTaskDataModel, GridTask>();
            Mapper.CreateMap<GridTask, GridTaskDataModel>();

            _timer = new Timer(TimerOnTick);

            var config = new Config(new AppSettings());
            _serviceManager = new GridManagementServiceProxy(new JsonServiceClient(config.UrlBase));

            GridTasks = new ObservableCollection<GridTaskDataModel>();

            AddNewTaskCommand = new DelegateCommand(AddNewTask);
            AddNewZipCommand = new DelegateCommand(AddNewZip);
            LaunchTaskCommand = new DelegateCommand(LaunchTask);
            RemoveTaskRepCommand = new DelegateCommand(RemoveTaskRep);
            AbortTaskCommand = new DelegateCommand(AbortTask);

            DataContext = this;

            _timer.Change(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(RefreshGridMs));

            Loaded += OnLoaded;
        }

        public GridTaskDataModel SelectedSlave
        {
            get { return _selectedSlave; }
            set
            {
                _selectedSlave = value;
                OnPropertyChanged("SelectedSlave");
            }
        }

        public GridTaskDataModel SelectedMaster
        {
            get { return _selectedMaster; }
            set
            {
                _selectedMaster = value;
                OnPropertyChanged("SelectedMaster");
            }
        }

        public DelegateCommand AddNewTaskCommand { get; set; }
        public DelegateCommand AddNewZipCommand { get; set; }
        public DelegateCommand LaunchTaskCommand { get; set; }
        public DelegateCommand RemoveTaskRepCommand { get; set; }
        public DelegateCommand AbortTaskCommand { get; set; }

        public ObservableCollection<GridTaskDataModel> GridTasks { get; set; }

        public string TaskName
        {
            get { return _taskName; }
            set
            {
                _taskName = value;
                OnPropertyChanged("TaskName");
            }
        }

        public string Data
        {
            get { return _data; }
            set
            {
                _data = value;
                OnPropertyChanged("Data");
            }
        }

        public string TaskFileZip
        {
            get { return _taskFileZip; }
            set
            {
                _taskFileZip = value;
                OnPropertyChanged("TaskFileZip");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void RemoveTaskRep(object o)
        {
            if (_collectionView.CurrentItem != null)
            {
                var task = (GridTaskDataModel) _collectionView.CurrentItem;

                try
                {
                    var response = _serviceManager.RemoveTaskRepository(task.TaskRepositoryName);
                    if (response.ResponseStatus != null && !string.IsNullOrWhiteSpace(response.ResponseStatus.ErrorCode))
                        MessageBox.Show(response.ResponseStatus.Message);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            _collectionView = CollectionViewSource.GetDefaultView(GridTasks);
            _collectionView.CurrentChanged += CollectionViewOnCurrentChanged;
        }

        private void AbortTask(object o)
        {
            if (SelectedMaster != null)
            {
                try
                {
                    var response = _serviceManager.AbortTask(SelectedMaster.Id);
                    if (response.ResponseStatus != null && !string.IsNullOrWhiteSpace(response.ResponseStatus.ErrorCode))
                        MessageBox.Show(response.ResponseStatus.Message);
                    else
                    {
                        //MessageBox.Show("Done");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void LaunchTask(object o)
        {
            if (SelectedMaster != null && SelectedSlave != null)
            {
                //RunTask(SelectedMaster.Id, SelectedSlave.Id);
                //return;

                var taskInfo = new TaskInfo
                {
                    PathAdvancements = @"C:\Dev\WpfApplication1 - Copy\GlFilesGenerationTest\bin\Release\advancements.txt",
                    ScanPeriod = new GetPeriodsWithNoSound
                    {
                        Day = DateTime.Today,
                    }
                };
                string jsonTest = new JsonNetSerializer().SerializeToString(taskInfo);

                _serviceManager.ScheduleTask(SelectedMaster.TaskRepositoryName, SelectedMaster.Id, SelectedSlave.Id, "30 0/1 * * * ?", jsonTest);

                return;
                var r = new Random();

                Data = File.ReadAllText("info.txt");

                //for (int i = 0; i < 1; i++)
                //    Task.Factory.StartNew(() =>
                //    {
                //        while (true)
                //        {
                //            RunTask(SelectedMaster.Id, SelectedSlave.Id);
                //            Thread.Sleep(TimeSpan.FromSeconds(r.Next(5)));
                //            break;
                //        }
                //    });

                Task.Factory.StartNew(() =>
                {
                    while (true)
                    {
                        RunTask("Id2", "Id1");
                        Thread.Sleep(TimeSpan.FromSeconds(r.Next(5)));
                        return;
                    }
                });

                Task.Factory.StartNew(() =>
                {
                    while (true)
                    {
                        RunTask("Id2", "Id1");
                        Thread.Sleep(TimeSpan.FromSeconds(r.Next(5)));
                        return;
                    }
                });

                //Task.Factory.StartNew(() =>
                //{
                //    while (true)
                //    {
                //        RunTask("NumericAudioSampleMaster", "NumericAudioSampleExtractionSlave");
                //        Thread.Sleep(TimeSpan.FromSeconds(r.Next(15)));
                //    }

                //});

                //Task.Factory.StartNew(() =>
                //{
                //    while (true)
                //    {
                //        RunTask("TestId2", "TestId1");
                //        Thread.Sleep(TimeSpan.FromSeconds(r.Next(5)));
                //        return;
                //    }
                //});

                //Task.Factory.StartNew(() =>
                //{
                //    while (true)
                //    {
                //        RunTask("Id3", "Id1");
                //        Thread.Sleep(TimeSpan.FromSeconds(r.Next(15)));
                //    }
                //});

                //Task.Factory.StartNew(() =>
                //{
                //    while (true)
                //    {
                //        RunTask("NumericGraphicSizeMaster", "NumericGraphicSizeSlave");
                //        Thread.Sleep(TimeSpan.FromSeconds(r.Next(15)));
                //    }
                //});
            }
        }

        private void RunTask(string masterID, string slaveID)
        {
            try
            {
                var response = _serviceManager.ExecuteTask(masterID, slaveID, Data);
                if (response.ResponseStatus != null && !string.IsNullOrWhiteSpace(response.ResponseStatus.ErrorCode))
                    MessageBox.Show(response.ResponseStatus.Message);
                else if (!string.IsNullOrWhiteSpace(response.JsonResponse))
                {
                    //MessageBox.Show(response.JsonResponse);
                }
                else
                {
                    //MessageBox.Show("Done");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void CollectionViewOnCurrentChanged(object sender, EventArgs eventArgs)
        {
            if (_collectionView.CurrentItem is GridTaskDataModel)
            {
                var task = (GridTaskDataModel) _collectionView.CurrentItem;
                if (task.Type == TaskType.Master || task.Type == TaskType.MasterLight)
                    SelectedMaster = task;

                if (task.Type == TaskType.Slave)
                    SelectedSlave = task;
            }
        }

        private void AddNewZip(object o)
        {
            var openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                TaskFileZip = openFileDialog.FileName;
                TaskName = Path.GetFileNameWithoutExtension(new FileInfo(TaskFileZip).Name);
            }
        }

        private void AddNewTask(object o)
        {
            if (!string.IsNullOrWhiteSpace(TaskName) && !string.IsNullOrWhiteSpace(TaskFileZip))
            {
                try
                {
                    var response = _serviceManager.AddTaskLibraries(TaskName, TaskFileZip);
                    if (response.ResponseStatus != null && !string.IsNullOrWhiteSpace(response.ResponseStatus.ErrorCode))
                        MessageBox.Show(response.ResponseStatus.Message);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void TimerOnTick(object state)
        {
            var client = new Client();

            try
            {
                var res = _serviceManager.GetGridTasks(null);
                var summary = _serviceManager.GetGridSummary(client);

                Dispatcher.Invoke(DispatcherPriority.Normal, new MainWindow.VoidDelegate(() => DisplayGridSummary(res, summary)));
            }
            catch (Exception ex)
            {
            }
        }

        private void DisplayGridSummary(GetGridTasksResponse res, GridSummary gridSummary)
        {
            var tasksId = new List<GridTaskDataModel>();

            foreach (var task in GridTasks)
            {
                if (res.Tasks.All(e => e.Id != task.Id))
                {
                    tasksId.Add(task);
                }
            }

            tasksId.ForEach(e => GridTasks.Remove(e));

            foreach (var task in res.Tasks)
            {
                if (GridTasks.Any(e => e.Id == task.Id))
                    continue;

                GridTasks.Add(Mapper.Map<GridTaskDataModel>(task));
            }

            foreach (var task in res.Tasks)
            { 
                var summary = gridSummary.TaskSummaries.FirstOrDefault(e => e.Progress.TaskName == task.Id);

                var taskSummary = GridTasks.First(e => e.Id == task.Id);
                taskSummary.State = task.State;

                if (summary != null)
                {
                    var summaries = gridSummary.TaskSummaries.Where(e => e.Progress.TaskName == task.Id).ToList();
                    foreach (var summary1 in summaries)
                    {
                        var el = taskSummary.TaskInstances.FirstOrDefault(e => e.Id == summary.Progress.TaskId.ToString());
                        if (el != null)
                        {
                            //
                        }
                    }

                    if (summary.Progress.StepsGoal > 0)
                        taskSummary.Completion = (double) summary.Progress.StepsCompleted/summary.Progress.StepsGoal;
                }
                else
                {
                    taskSummary.Completion = 0;
                }
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public class GetPeriodsWithNoSound
        {
            public GetPeriodsWithNoSound()
            {
                FilterSec = 60;
            }

            public DateTime Day { get; set; }
            public int FilterSec { get; set; }
            public string Media { get; set; }

            public TimeSpan? Begin { get; set; }
            public TimeSpan? End { get; set; }
        }

        public class TaskInfo
        {
            public GetPeriodsWithNoSound ScanPeriod { get; set; }
            public string PathAdvancements { get; set; }
        }
    }
}