using System;
using System.Runtime.Serialization;

namespace GridAgentSharedLib.Clients
{
    /// <summary>
    /// Executes, what are normally, remote SlaveTasks 
    /// on behalf of a <see cref="MasterTask"/>.
    /// </summary>
    [DataContract]
    [Serializable]
    public class Agent : Client, IAgent
    {
        public Agent(IClient info) : base(info)
        {
        }

        public Agent()
        {
        }

        #region IAgent Members

        /// <summary>
        /// Gets or sets the processing speed in megaFlops 
        /// that the client indicates it is processing.
        /// </summary>
        /// <value>The megaFlops.</value>
        [DataMember]
        public long MFlops { get; set; }

        /// <summary>
        /// Gets or sets the bandwidth in Kilobytes per second
        /// that the client is capable of transfering from
        /// the server.
        /// </summary>
        /// <value>The bandwidth in KiloBytes per second.</value>
        [DataMember]
        public double BandwidthKBps { get; set; }

        /// <summary>
        /// Gets or sets the number of processors.
        /// </summary>
        /// <value>The processor count.</value>
        [DataMember]
        public double ProcessorCount { get; set; }

        [DataMember]
        public double TotalPhysicalMemory { get; set; }

        #endregion

        public override string ToString()
        {
            return base.ToString() + string.Format(", MFlops: {0}, BandwidthKBps: {1}", MFlops, BandwidthKBps);
        }
    }
}