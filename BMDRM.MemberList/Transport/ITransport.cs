using System.Net;
using System.Net.Sockets;

namespace BMDRM.MemberList.Transport
{
    /// <summary>
    /// Transport is used to abstract over communicating with other peers. The packet
    /// interface is assumed to be best-effort and the stream interface is assumed to
    /// be reliable.
    /// </summary>
    public interface ITransport
    {
        /// <summary>
        /// FinalAdvertiseAddr is given the user's configured values (which
        /// might be empty) and returns the desired IP and port to advertise to
        /// the rest of the cluster.
        /// </summary>
        Task<(IPAddress ip, int port)> FinalAdvertiseAddrAsync(string ip, int port);

        /// <summary>
        /// WriteTo is a packet-oriented interface that fires off the given
        /// payload to the given address in a connectionless fashion. This should
        /// return a time stamp that's as close as possible to when the packet
        /// was transmitted to help make accurate RTT measurements during probes.
        /// 
        /// This is similar to Socket UDP operations, though we didn't want to expose
        /// that full set of required methods to keep assumptions about the
        /// underlying plumbing to a minimum. We also treat the address here as a
        /// string, similar to Socket.Connect, so it's network neutral, so this usually is
        /// in the form of "host:port".
        /// </summary>
        Task<DateTime> WriteToAsync(byte[] buffer, string addr);

        /// <summary>
        /// PacketStream returns an IAsyncEnumerable that can be used to receive incoming
        /// packets from other peers. How this is set up for listening is left as
        /// an exercise for the concrete transport implementations.
        /// </summary>
        IAsyncEnumerable<Packet> PacketStream { get; }

        /// <summary>
        /// DialTimeout is used to create a connection that allows us to perform
        /// two-way communication with a peer. This is generally more expensive
        /// than packet connections so is used for more infrequent operations
        /// such as anti-entropy or fallback probes if the packet-oriented probe
        /// failed.
        /// </summary>
        Task<Socket> DialTimeoutAsync(string addr, TimeSpan timeout);

        /// <summary>
        /// StreamStream returns an IAsyncEnumerable that can be used to handle incoming stream
        /// connections from other peers. How this is set up for listening is
        /// left as an exercise for the concrete transport implementations.
        /// </summary>
        IAsyncEnumerable<Socket> StreamStream { get; }

        /// <summary>
        /// Shutdown is called when memberlist is shutting down; this gives the
        /// transport a chance to clean up any listeners.
        /// </summary>
        Task ShutdownAsync();
    }
}
