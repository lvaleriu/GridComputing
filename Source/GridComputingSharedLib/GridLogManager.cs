#region

using System;

#endregion

namespace GridComputingSharedLib
{
    public static class GridLogManager
    {
        public static IGridLog GetLogger(Type type)
        {
            return new GridLog();
        }
    }
}