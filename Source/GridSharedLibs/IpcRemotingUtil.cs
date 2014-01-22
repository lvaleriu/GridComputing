using System;
using System.Collections;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters;

namespace GridSharedLibs
{
    /// <summary>
    /// Utility methods to support .NET Remoting with IPC/TCP channels
    /// </summary>
    public static class RemotingUtil
    {
        /// <summary>
        /// Return a new client/server IpcChannel
        /// </summary>
        /// <param name="portName">IPC port name</param>
        /// <returns>IpcChannel</returns>

        public static IpcChannel CreateIpcChannel(string portName)
        {
            /*Note that both sides use IpcChannel so that the server can invoke callbacks in the client. 
             * If you don't need callbacks, you could use IpcServerChannel and IpcClientChannel instead of IpcChannel. 
             * If you do that, you need to make sure you set the TypeFilterLevel and authorizedGroup on the IpcServerChannel as is done above for the IpcChannel.
             * 
             * By default, user-defined types will not be deserialized, to prevent deserialization-based attacks by malicious clients. To disable this "feature", one must set the TypeFilterLevel to Full.
            */
            var serverSinkProvider = new BinaryServerFormatterSinkProvider {TypeFilterLevel = TypeFilterLevel.Full};

            IDictionary properties = new Hashtable();
            properties["portName"] = portName;
            // here enter unique channel name
            properties["name"] = portName;
            /*
             * My service runs as the LocalSystem user, whereas the client application runs in the logged-in user's security context. By default, the user's account would not be able open the IPC port 
             * that the service creates. The fix to this is to set the channel's authorizedGroup to the name of a user group that is allowed to open the port.
             * */
            //properties["authorizedGroup"] = "Everyone";

            // Create the server channel.
            // Pass the properties for the port setting and the server provider in the server chain argument. (Client remains null here.)
            //new IpcServerChannel(props, serverProvider) 
            // new IpcClientChannel(props, clientProvider)
            return new IpcChannel(properties, null, serverSinkProvider);
        }

        public static TcpChannel CreateTcpChannel(string port)
        {
            // Creating a custom formatter for a TcpChannel sink chain.
            var serverSinkProvider = new BinaryServerFormatterSinkProvider { TypeFilterLevel = TypeFilterLevel.Full };

            IDictionary properties = new Hashtable();
            properties["port"] = port;

            return new TcpChannel(properties, null, serverSinkProvider);
        }

        public static IChannel CreateClientTcpChannel()
        {
            // Creating a custom formatter for a TcpChannel sink chain.
            var clientProvider = new BinaryClientFormatterSinkProvider();
            var serverProvider = new BinaryServerFormatterSinkProvider {TypeFilterLevel = TypeFilterLevel.Full};

            // Creating the IDictionary to set the port on the channel instance.
            IDictionary props = new Hashtable();

            props["port"] = 0;

            return new TcpChannel(props, clientProvider, serverProvider);
        }

        /// <summary>
        /// Return a new client/server IpcChannel with a unique IPC port name
        /// </summary>
        /// <returns>IpcChannel</returns>
        public static IpcChannel CreateIpcChannelWithUniquePortName()
        {
            return CreateIpcChannel(Guid.NewGuid().ToString());
        }
    }
}