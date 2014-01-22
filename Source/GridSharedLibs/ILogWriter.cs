namespace GridSharedLibs
{
    /// <summary>
    /// Used for writing messages on the process of the exectuion of the task instead of the main grid manager's process
    /// </summary>
    public interface ILogWriter
    {
        void Log(string message);
    }
}