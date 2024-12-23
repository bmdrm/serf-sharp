using System.Net;

namespace BMDRM.MemberList.Transport
{
    /// <summary>
    /// Packet is used to provide some metadata about incoming packets from peers
    /// over a packet connection, as well as the packet payload.
    /// </summary>
    public class Packet
    {
        /// <summary>
        /// Buffer has the raw contents of the packet.
        /// </summary>
        public byte[] Buffer { get; }

        /// <summary>
        /// From has the address of the peer. This is an actual EndPoint so we
        /// can expose some concrete details about incoming packets.
        /// </summary>
        public EndPoint From { get; }

        /// <summary>
        /// Timestamp is the time when the packet was received. This should be
        /// taken as close as possible to the actual receipt time to help make an
        /// accurate RTT measurement during probes.
        /// </summary>
        public DateTime Timestamp { get; }

        public Packet(byte[] buffer, EndPoint from, DateTime timestamp)
        {
            Buffer = buffer;
            From = from;
            Timestamp = timestamp;
        }
    }
}
