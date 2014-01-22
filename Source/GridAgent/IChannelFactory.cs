#region

using GridAgentSharedLib;

#endregion

namespace GridAgent
{
    public interface IChannelFactory
    {
        ICommunicationServerFactory GetCommunicationServerFactory(string pluginPath);
        void CloseChannel();
    }
}