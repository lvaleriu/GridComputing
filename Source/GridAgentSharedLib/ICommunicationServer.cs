namespace GridAgentSharedLib
{
    public interface ICommunicationServer
    {
        T Send<T>(object request);
    }
}