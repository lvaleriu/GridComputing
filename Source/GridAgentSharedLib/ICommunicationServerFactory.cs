namespace GridAgentSharedLib
{
    public interface ICommunicationServerFactory
    {
        ICommunicationServer GetCommunicationServer(int id);
        void Close();
    }
}