using System;
using System.Runtime.Serialization;

namespace GridAgentSharedLib.Clients
{
    /// <summary>
    /// Represents a remote application,
    /// utilizing the services of the Grid.
    /// </summary>
    [DataContract]
    [Serializable]
    public class Client : IClient
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Client"/> class.
        /// </summary>
        /// <param name="info">The info to copy to the new instance.</param>
        public Client(IClient info)
        {
            MachineName = info.MachineName;
            Url = info.Url;
            UserName = info.UserName;
            IPAddress = info.IPAddress;
            Id = info.Id;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Client"/> class.
        /// </summary>
        public Client()
        {
        }

        #region IClient Members

        /// <summary>
        /// Gets or sets the unique id of this instance.
        /// </summary>
        /// <value>The id.</value>
        [DataMember]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets the name of the user that the request
        /// was made under.
        /// </summary>
        /// <value>The name of the user.</value>
        [DataMember]
        public string UserName { get; set; }

        /// <summary>
        /// Gets the name of the machine that the request for the log entry
        /// was made.
        /// </summary>
        /// <value>The name of the machine.</value>
        [DataMember]
        public string MachineName { get; set; }

        /// <summary>
        /// Gets or sets the URL from where the log request was made.
        /// </summary>
        /// <value>The URL of the log request.</value>
        [DataMember]
        public string Url { get; set; }

        /// <summary>
        /// Gets the ip address of the client.
        /// </summary>
        /// <value>The ip address of the client.</value>
        [DataMember]
        public string IPAddress { get; set; }

        #endregion

        public override string ToString()
        {
            return string.Format("Id: {0}, MachineName: {1}, IPAddress: {2}, Url:{3}, UserName: {4}",
                                 Id, MachineName, IPAddress, Url, UserName);
        }

        public override bool Equals(object obj)
        {
            var info = obj as Client;
            return info != null && info.Id == Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}