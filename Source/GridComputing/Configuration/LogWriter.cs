#region

using System;
using GridComputingSharedLib;
using GridSharedLibs;

#endregion

namespace GridComputing.Configuration
{
    public class LogWriter : IGridLog
    {
        private readonly ILogWriter _logWriter;

        public LogWriter(ILogWriter logWriter)
        {
            _logWriter = logWriter;
        }

        public void Info(string info)
        {
            _logWriter.Log("Info: " + info);
        }

        public void Error(string error, Exception ex)
        {
            _logWriter.Log("Error: " + error + ": " + GridLog.SerializeException(ex));
        }

        public void InfoFormat(string info, params object[] args)
        {
            _logWriter.Log(string.Format("Info: " + info, args));
        }

        public void Warn(string warning)
        {
            _logWriter.Log("Warn: " + warning);
        }

        public void WarnFormat(string s, params object[] args)
        {
            _logWriter.Log(string.Format("Warn: " + s, args));
        }

        public void Warn(string warning, Exception ex)
        {
            _logWriter.Log("Warn: " + warning + ". Error: " + GridLog.SerializeException(ex));
        }
    }
}