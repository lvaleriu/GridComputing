using System;
using GridSharedLibs.ClientServices;
using ServiceStack.Logging;

namespace GridAgent
{
    /// <summary>
    /// Gathers performance statistics for a client.
    /// </summary>
    public class Performance
    {
        private readonly ILog Log = LogManager.GetLogger(typeof (Performance));

        /// <summary>
        /// Measures the processor speed in megaFLOPS.
        /// Not particularly accurate, but it provides 
        /// a rough indication of what the client can do.
        /// </summary>
        /// <returns>The processor speed in megaFLOPS.</returns>
        public long MeasureMFlops()
        {
            double x = 0, y = 0, z = 0;
            DateTime startTime = DateTime.Now;
            /* 60 million fp operations? */
            for (int index = 0; index < 10000000; index++)
            {
                x = 2.34 + index;
                y = 5.67 + index;
                z = (x*x) + (y*y) + index;
            }
            DateTime endTime = DateTime.Now;

            TimeSpan span = endTime - startTime;

            return span.Ticks/60;
        }

        /// <summary>
        /// Measures the bandwidth of the connection
        /// with the server by calling a web service
        /// method that returns a large message
        /// of a predetermined size.
        /// This method must not be executed on the UI thread.
        /// </summary>
        /// <returns>The download rate of the client in KiloBytes.
        /// </returns>
        public long MeasureBandwidthKBps(IGridService gridService)
        {
            const long resultDefault = 0;
            long result = resultDefault;

            DateTime begun = DateTime.Now;

            try
            {
#if Silverlight
                ThreadSynchronizer.Current.RaiseExceptionIfOnSameThread();
#endif
                byte[] payload = gridService.Download100KB();
            }
            catch (Exception ex)
            {
                Log.Error("Unable to download speed test.", ex);
                return result;
            }

            DateTime ended = DateTime.Now;

            TimeSpan span = ended - begun;
            if (span.TotalMilliseconds > 0)
            {
                result = 100000/(long) span.TotalMilliseconds;
            }

            return result;
        }
    }
}