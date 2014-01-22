using System;

namespace GridComputingSharedLib
{
    public interface IGridLog
    {
        void Info(string info);
        void Error(string error, Exception ex);
        void InfoFormat(string info, params object[] args);
        void Warn(string warning);
        void WarnFormat(string warning, params object[] args);
        void Warn(string warning, Exception ex);
    }
}