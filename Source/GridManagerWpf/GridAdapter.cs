using System;
using System.Diagnostics;
using Decav.Windows.Controls.LineGraph;

namespace GridManagerWpf
{
    /// <summary>
    /// Used for displaying graphs of grid statitics.
    /// </summary>
    public class GridAdapter : TickerAdapter
    {
        readonly Stopwatch _stopwatch = new Stopwatch();
        private const double VirtualSecondsPerSecond = 500000;
        TimeSpan _realTimePassed = TimeSpan.Zero;
        TimeSpan _virtualTimePassed = TimeSpan.Zero;

        protected override void OnStarted(EventArgs e)
        {
            _stopwatch.Start();

            base.OnStarted(e);
        }

        public void AddValue(string id, decimal value)
        {
            TimeSpan realTime = _stopwatch.Elapsed;
            TimeSpan virtualTime = TimeSpan.FromSeconds(realTime.TotalSeconds * VirtualSecondsPerSecond);

            var tick = new GraphTick(
                realTime + _realTimePassed,
                virtualTime + _virtualTimePassed,
                value);

            NotifyNewTick(id, tick);

            _realTimePassed += realTime;
            _virtualTimePassed += virtualTime;
            _stopwatch.Reset();
            _stopwatch.Start();
        }

        public void AddGraph(string id, string title)
        {
            AddSecurity(new Security(id, title));
        }
    }
}