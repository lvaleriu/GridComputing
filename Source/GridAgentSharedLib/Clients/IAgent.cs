namespace GridAgentSharedLib.Clients
{
    /// <summary>
    /// Executes, what are normally, remote SlaveTasks 
    /// on behalf of a <see cref="MasterTask"/>.
    /// </summary>
    public interface IAgent : IClient
    {
        /// <summary>
        /// Gets or sets the processing speed in megaFlops 
        /// that the client indicates it is processing.
        /// </summary>
        /// <value>The megaFlops.</value>
        long MFlops { get; set; }

        /// <summary>
        /// Gets or sets the bandwidth in Kilobytes per second
        /// that the client is capable of transfering from
        /// the server.
        /// </summary>
        /// <value>The bandwidth in KiloBytes per second.</value>
        double BandwidthKBps { get; set; }

        /// <summary>
        /// Gets or sets the number of processors.
        /// </summary>
        /// <value>The processor count.</value>
        double ProcessorCount { get; set; }

        double TotalPhysicalMemory { get; set; }
    }
}