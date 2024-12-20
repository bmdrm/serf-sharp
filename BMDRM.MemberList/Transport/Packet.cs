using System;
using System.Net;

namespace BMDRM.MemberList.Transport
{
    /// <summary>
    /// Provides metadata about incoming packets from peers over a packet connection.
    /// </summary>
    public class Packet
    {
        /// <summary>
        /// Raw contents of the packet.
        /// </summary>
        public byte[] Buffer { get; set; }

        /// <summary>
        /// Address of the peer.
        /// </summary>
        public EndPoint From { get; set; }

        /// <summary>
        /// Time when the packet was received.
        /// Used to make accurate RTT measurements during probes.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}
