using System;
using System.Net;
using System.Threading.Tasks;

namespace BMDRM.MemberList.Transport
{
    /// <summary>
    /// Deprecated: IngestionAwareTransport is not used and may be removed in a future version.
    /// Define the interface locally instead of referencing this exported interface.
    /// </summary>
    [Obsolete("IngestionAwareTransport is not used and may be removed in a future version.")]
    public interface IIngestionAwareTransport
    {
        /// <summary>
        /// Ingests a packet from a network connection.
        /// </summary>
        Task IngestPacketAsync(INetworkStream connection, EndPoint address, DateTime timestamp, bool shouldClose);

        /// <summary>
        /// Ingests a stream from a network connection.
        /// </summary>
        Task IngestStreamAsync(INetworkStream connection);
    }
}
