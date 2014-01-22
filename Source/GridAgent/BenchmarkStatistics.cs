using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace GridAgent
{
    public class BenchmarkStatistics : IDisposable
    {
        #region Fields

        private readonly CancellationTokenSource _source;
        private readonly Task _getCpuUsage;
        private readonly ManualResetEventSlim _slim;
        private double _procCpuUsage;
        private readonly Stopwatch _sw;
        private TimeSpan _elapsedTime;

        #endregion

        public BenchmarkStatistics(int procId)
        {
            _sw = new Stopwatch();
            _sw.Start();

            _source = new CancellationTokenSource();
            _procCpuUsage = 0;
   
            _slim = new ManualResetEventSlim(false);

            _getCpuUsage = Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (_source.IsCancellationRequested)
                        break;

                    Process[] runningNow = Process.GetProcesses();
                    foreach (var process in runningNow)
                    {
                        if (process.Id != procId)
                            continue;

                        using (PerformanceCounter cpuUsage = new PerformanceCounter("Process", "% Processor Time", "_Total"))
                        using (PerformanceCounter pcProcess = new PerformanceCounter("Process", "% Processor Time", process.ProcessName))
                        using (PerformanceCounter memProcess = new PerformanceCounter("Memory", "Available MBytes"))
                        {
                            // Prime the Performance Counters
                            pcProcess.NextValue();
                            Thread.Sleep(100);
                            cpuUsage.NextValue();

                            double cpuUse = Math.Round(pcProcess.NextValue() / cpuUsage.NextValue() * 100, 2);

                            // Check for Not-A-Number (Division by Zero)
                            if (double.IsNaN(cpuUse) || double.IsInfinity(cpuUse))
                                cpuUse = 0;

                            // Get Process Memory Usage
                            double memUseage = process.PrivateMemorySize64 / 1048576;

                            // Get Total RAM free
                            float mem = memProcess.NextValue();

                            _procCpuUsage = cpuUse;

                            _slim.Set();
                        }
                    }
                }

                _slim.Set();

            }, _source.Token);
        }

        public double ProcCpuUsage
        {
            get { return _procCpuUsage; }
        }

        public TimeSpan ElapsedTime
        {
            get { return _elapsedTime; }
        }

        public void Dispose()
        {
            _source.Cancel();
            try
            {
                _getCpuUsage.Wait();
            }
            catch (Exception)
            {
            }
            _slim.Wait();

            _sw.Stop();

            _elapsedTime = _sw.Elapsed;
        }
    }
}