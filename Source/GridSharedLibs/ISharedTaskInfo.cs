#region

using GridAgentSharedLib.TypesCreation;
using GridComputingSharedLib.TypesCreation;

#endregion

namespace GridSharedLibs
{
    public interface ISharedTaskInfo : ICreateMasterInstance, ICreateSlaveInstance, ILogWriter
    {
        void Ping();
        void SetPingChecking(bool enable);
    }
}