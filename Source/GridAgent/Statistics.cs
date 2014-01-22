using System;
using System.Management;
using GridSharedLibs.ClientServices;

namespace GridAgent
{
    public sealed class Statistics
    {
        private readonly Performance _performance;
        private readonly IGridService _instance;

        public Statistics(IGridService instance)
        {
            _performance = new Performance();
            _instance = instance;
        }

        private uint _cpuSpeedMhz;
        private readonly object _mcpuSpeedLock = new object();

        private uint GetCpuSpeedMhz()
        {
            lock (_mcpuSpeedLock)
            {
                if (_cpuSpeedMhz == 0)
                    _cpuSpeedMhz = CpuSpeed();
            }
            return _cpuSpeedMhz;
        }

        private uint CpuSpeed()
        {
            var mo = new ManagementObject("Win32_Processor.DeviceID='CPU0'");
            var sp = (uint)(mo["CurrentClockSpeed"]);
            mo.Dispose();
            return sp;
        }

        private long _mflops = -1;
        private readonly object _mflopsLock = new object();

        public long GetMFlops()
        {
            lock (_mflopsLock)
            {
                if (_mflops == -1)
                    _mflops = _performance.MeasureMFlops();
            }
            return _mflops;
        }

        private long _bandwidthKBs = -1;
        private readonly object _bandwidthKBsLock = new object();

        public long GetBandwidthKBs()
        {
            lock (_bandwidthKBsLock)
            {
                if (_bandwidthKBs <= 0)
                    _bandwidthKBs = _performance.MeasureBandwidthKBps(_instance);
            }
            return _bandwidthKBs;
        }

        public long GetTotalPhysicalMemoryKBs()
        {
            /*
             * Win32_LogicalMemoryConfiguration is not available from Vista onwards. Replace with CIM_OperatingSystem and use TotalVisibleMemorySize, 
             * TotalVirtualMemorySize etc. instead. The code above throws an exception on Win7 and probably on Vista as well
             */

            ObjectQuery winQuery = new ObjectQuery("SELECT * FROM CIM_OperatingSystem");

            ManagementObjectSearcher searcher = new ManagementObjectSearcher(winQuery);

            foreach (ManagementObject item in searcher.Get())
            {
                return Convert.ToInt64(item["TotalVisibleMemorySize"]);
            }

            return 0;
        }
    }
}