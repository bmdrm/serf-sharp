using System;
using System.Net;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BMDRM.MemberList.Transport
{
    /// <summary>
    /// Abstracts communication with other peers. The packet interface is assumed to be best-effort
    /// and the stream interface is assumed to be reliable.
    /// </summary>
    public interface ITransport
    {
        /// <summary>
        /// Returns the desired IP and port to advertise to the rest of the cluster.
        /// </summary>
        /// <param name="ip">User's configured IP value (might be empty)</param>
        /// <param name="port">User's configured port value</param>
        /// <returns>A tuple containing the IP address and port to advertise</returns>
        Task<(IPAddress Address, int Port)> FinalAdvertiseAddressAsync(string ip, int port);

        /// <summary>
        /// Sends the given payload to the given address in a connectionless fashion.
        /// </summary>
        /// <param name="buffer">The data to send</param>
        /// <param name="address">The target address in "host:port" format</param>
        /// <returns>Timestamp when the packet was transmitted</returns>
        Task<DateTime> WriteToAsync(byte[] buffer, string address);

        /// <summary>
        /// Returns a channel that can be read to receive incoming packets from other peers.
        /// </summary>
        Channel<Packet> PacketChannel { get; }

        /// <summary>
        /// Creates a connection for two-way communication with a peer.
        /// </summary>
        /// <param name="address">The target address in "host:port" format</param>
        /// <param name="timeout">Connection timeout duration</param>
        Task<INetworkStream> DialAsync(string address, TimeSpan timeout);

        /// <summary>
        /// Returns a channel that can be read to handle incoming stream connections from other peers.
        /// </summary>
        Channel<INetworkStream> StreamChannel { get; }

        /// <summary>
        /// Shuts down the transport and cleans up any listeners.
        /// </summary>
        Task ShutdownAsync();
    }
}
