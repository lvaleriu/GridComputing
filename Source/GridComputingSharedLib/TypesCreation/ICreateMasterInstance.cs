using GridAgentSharedLib.TypesCreation;

namespace GridComputingSharedLib.TypesCreation
{
    public interface ICreateMasterInstance : ICreateInstance
    {
        IMasterTask CreateMasterTask(string assemblyQualifiedName, string dllLocation);
        IFullMasterTask CreateFullMasterTask(string assemblyQualifiedName, string dllLocation);
    }
}