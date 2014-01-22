#region

using System;

#endregion

namespace GridAgentSharedLib.TypesCreation
{
    public interface ICreateInstance
    {
        void Close();
        int GetProcessId();

        AppDomain GetExecutingDomain();
    }
}