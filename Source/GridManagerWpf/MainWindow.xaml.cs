#region

using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using Decav.Windows.Controls.LineGraph;
using GridAgentSharedLib.Clients;
using GridComputing;
using GridComputingServices.ClientServices;
using GridSharedLibs;
using ServiceStack.Configuration;
using ServiceStack.Logging;
using ServiceStack.Logging.Support.Logging;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceModel.Serialization;

#endregion

namespace GridManagerWpf
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        #region Fields

        /* Used for the graphAdapter. */
        private const string GflopsGraphId = "gigaFLOPS";
        private const string BandwidthGraphId = "Bandwidth";
        private const string CpuSpeedGraphId = "Cpu Speed";

        /* Time to retrieve new graph summary in milliseconds. */
        /* TODO: make the interval time configurable. */
        private const int RefreshGridMs = 1000;
        private readonly GridAdapter _graphAdapter;
        private readonly ILog _log;
        private readonly object _retrievingInfoLock = new object();
        private readonly IGridManagementService _serviceManager;
        private readonly DispatcherTimer _timer = new DispatcherTimer();
        private int _graphRow;
        private bool _retrieveFailed;
        private int _retrievedCount;
        private bool _retrievingInfo;

        #endregion

        #region Delegates

        public delegate void VoidDelegate();

        #endregion

        public MainWindow()
        {
            LogManager.LogFactory = new DebugLogFactory();
            _log = LogManager.GetLogger(typeof (MainWindow));

            InitializeComponent();
            Loaded += MainWindowLoaded;
            Closed += MainWindowClosed;

            var config = new Config(new AppSettings());

            JsonDataContractSerializer.UseSerializer(new JsonNetSerializer());
            _serviceManager = new GridManagementServiceProxy(new JsonServiceClient(config.UrlBase) {Timeout = TimeSpan.FromMinutes(1)});

            _graphAdapter = new GridAdapter();
            _graphAdapter.TickerAdded += GraphAdapterTickerAdded;
            _graphAdapter.SecurityAdded += GraphAdapterSecurityAdded;
            _graphAdapter.Start();

            _graphAdapter.SetBinding(TickerAdapter.GraphDurationProperty,
                new Binding("Value")
                {
                    Source = TimeSlider,
                    Converter = new DoubleToTimeSpanConverter(),
                    ConverterParameter = TimeSpan.FromDays(2000)
                });
        }

        private void MainWindowClosed(object sender, EventArgs e)
        {
            _timer.Stop();
        }

        private void MainWindowLoaded(object sender, RoutedEventArgs e)
        {
            _log.Info("Manager started");

            _graphAdapter.AddGraph(GflopsGraphId, "gigaFLOPS");
            _graphAdapter.AddGraph(BandwidthGraphId, "Bandwidth (MB/sec)");
            _graphAdapter.AddGraph(CpuSpeedGraphId, "CPU Speed (Mhz)");

            DisplayMessage("Ready");

            _timer.Interval = TimeSpan.FromMilliseconds(RefreshGridMs);
            _timer.Tick += TimerTick;
            _timer.Start();
        }

        private void DisplayMessage(string message)
        {
            listBox_Messages.Items.Add(message);
        }

        private void TimerTick(object sender, EventArgs e)
        {
            RetrieveGridSummary();
        }

        private void RetrieveGridSummary()
        {
            /* Prevent calls from backing up. */
            lock (_retrievingInfoLock)
            {
                if (_retrievingInfo)
                {
                    return;
                }

                _retrievingInfo = true;
            }

            try
            {
                _log.Debug("Retrieving grid: " + ++_retrievedCount);
                RetrieveGridSummaryAux();
            }
            catch (Exception ex)
            {
                _log.Error("Unable to retrieve GridSummary.", ex);
            }
        }

        private void RetrieveGridSummaryAux()
        {
            ThreadPool.QueueUserWorkItem(RetrieveGridSummary);
        }

        private void RetrieveGridSummary(object state)
        {
            try
            {
                var client = new Client();
                GridSummary gridSummary;

                try
                {
                    gridSummary = _serviceManager.GetGridSummary(client);
                }
                catch (Exception ex)
                {
                    if (!_retrieveFailed)
                    {
                        _retrieveFailed = true;
                        const string errorMessage = "Unable to retrieve grid info.";
                        _log.Error(errorMessage, ex);
                        /* Must be performed on main UI thread. */
                        Dispatcher.Invoke(DispatcherPriority.Normal, new VoidDelegate(() => DisplayMessage(errorMessage + "  " + ex)));
                    }
                    return;
                }

                if (gridSummary != null)
                {
                    /* Must be performed on main UI thread. */
                    Dispatcher.Invoke(DispatcherPriority.Normal, new VoidDelegate(() => DisplayGridSummary(gridSummary)));
                }
            }
            finally
            {
                lock (_retrievingInfoLock)
                {
                    _retrievingInfo = false;
                }
            }
        }

        private void DisplayGridSummary(GridSummary gridSummary)
        {
            int agentCountTotal = 0;

            double bandwidthKBpsTotal = 0;
            long mflopsTotal = 0;

            long progressTotal = 0;
            long progressGoalTotal = 0;

            foreach (TaskSummary summary in gridSummary.TaskSummaries)
            {
                agentCountTotal += summary.AgentCount;
                bandwidthKBpsTotal += summary.BandwidthKBps > 0 ? summary.BandwidthKBps : 0;
                mflopsTotal += summary.MFlops;
                if (summary.Progress != null)
                {
                    progressTotal += summary.Progress.StepsCompleted;
                    progressGoalTotal += summary.Progress.StepsGoal;
                }
            }

            mflopsTotal = gridSummary.ConnectedAgents.Sum(a => a.MFlops);
            bandwidthKBpsTotal = gridSummary.ConnectedAgents.Sum(a => a.BandwidthKBps);
            
            label_AgentCountTotal.Content = agentCountTotal;
            label_WorkingAgentCountTotal.Content = gridSummary.ConnectedAgents.Count;

            double percentComplete = (progressGoalTotal != 0) ? progressTotal/(double) progressGoalTotal : 0;
            label_PercentCompleteTotal.Content = string.Format("{0:0%}", percentComplete);

            try
            {
                /* Convert MFLOPS to GFLOPS. */
                _graphAdapter.AddValue(GflopsGraphId, mflopsTotal/1000);
                _graphAdapter.AddValue(BandwidthGraphId, (decimal) (bandwidthKBpsTotal/1000f));
            }
            catch (Exception ex)
            {
                _log.Error("Exception thrown by Decav graph.", ex);
            }

            _retrieveFailed = false;
        }

        private void GraphAdapterSecurityAdded(object sender, SecurityAddedEventArgs e)
        {
            _graphAdapter.CreateTicker(e.Security);
        }

        private void GraphAdapterTickerAdded(object sender, TickerAddedEventArgs e)
        {
            MainGrid.Children.Add(e.Ticker);
            int currentRow = _graphRow++;
            const int currentColumn = 0;

            e.Ticker.SetValue(Grid.RowProperty, currentRow);
            e.Ticker.SetValue(Grid.ColumnProperty, currentColumn);

            /* Stretch to fill the cell. */
            e.Ticker.Height = Double.NaN;
            e.Ticker.Width = Double.NaN;
            e.Ticker.HorizontalAlignment = HorizontalAlignment.Stretch;
            e.Ticker.VerticalAlignment = VerticalAlignment.Stretch;
            e.Ticker.Margin = new Thickness(10, 10, 10, 10);
        }
    }
}