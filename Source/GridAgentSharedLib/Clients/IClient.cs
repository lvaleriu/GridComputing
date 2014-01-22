using System;

namespace GridAgentSharedLib.Clients
{
    /// <summary>
    /// Represents a remote application,
    /// utilizing the services of the Grid.
    /// </summary>
    public interface IClient
    {
        /// <summary>
        /// Gets or sets the unique id of this instance.
        /// </summary>
        /// <value>The id.</value>
        Guid Id { get; }

        /// <summary>
        /// Gets or sets the URL of the client.
        /// </summary>
        /// <value>The URL of the client.</value>
        string Url { get; }

        /// <summary>
        /// Gets the name of the user that the request
        /// was made under.
        /// </summary>
        /// <value>The name of the user.</value>
        string UserName { get; }

        /// <summary>
        /// Gets the name of the machine that the request for the log entry
        /// was made.
        /// </summary>
        /// <value>The name of the machine.</value>
        string MachineName { get; }

        /// <summary>
        /// Gets the ip address of the client.
        /// </summary>
        /// <value>The ip address of the client.</value>
        string IPAddress { get; }
    }
}