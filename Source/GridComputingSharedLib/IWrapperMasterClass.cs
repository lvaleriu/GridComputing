namespace GridComputingSharedLib
{
    public interface IWrapperMasterClass : IMasterTask
    {
        void RemoveHandlers();
        void FireFailedCompletion(string ex);
    }

    public interface IWrapperFullMasterClass : IWrapperMasterClass, IFullMasterTask
    {
        
    }
}