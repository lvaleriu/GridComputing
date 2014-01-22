namespace GridSharedLibs
{
    public enum GridTaskState
    {
        WaitingForExecution,
        Running,
        RunningBeforeRemoval,
        WaitingForRemoval,
        CannotBeLoaded,
    }
}