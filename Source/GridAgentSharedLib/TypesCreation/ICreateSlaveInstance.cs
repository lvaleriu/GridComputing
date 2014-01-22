namespace GridAgentSharedLib.TypesCreation
{
    public interface ICreateSlaveInstance : ICreateInstance
    {
        ISlaveTask CreateSlaveTask(string assemblyQualifiedName, string dllLocation);
    }
}