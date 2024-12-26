using System;
using System.Collections.Generic;
using System.Net;
using BMDRM.MemberList.Transport;
using BMDRM.MemberList.Delegates;

namespace BMDRM.MemberList
{
    /// <summary>
    /// Configuration for MemberList node
    /// </summary>
    public class Config
    {
        /// <summary>
        /// The name of this node. This must be unique in the cluster.
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// Transport is a hook for providing custom code to communicate with other nodes.
        /// </summary>
        public ITransport Transport { get; set; } = null!;

        /// <summary>
        /// Optional set of bytes to include on the outside of each packet and stream.
        /// If gossip encryption is enabled, this is treated as GCM authenticated data.
        /// </summary>
        public string Label { get; set; } = "";

        /// <summary>
        /// Skips the check that inbound packets and gossip streams need to be label prefixed.
        /// </summary>
        public bool SkipInboundLabelCheck { get; set; }

        /// <summary>
        /// Address to bind to for both UDP and TCP gossip
        /// </summary>
        public string BindAddr { get; set; } = "";

        /// <summary>
        /// Port to bind to for both UDP and TCP gossip
        /// </summary>
        public int BindPort { get; set; }

        /// <summary>
        /// Address to advertise to other cluster members. Used for NAT traversal.
        /// </summary>
        public string AdvertiseAddr { get; set; } = "";

        /// <summary>
        /// Port to advertise to other cluster members.
        /// </summary>
        public int AdvertisePort { get; set; }

        /// <summary>
        /// Protocol version that we will speak
        /// </summary>
        public byte ProtocolVersion { get; set; }

        /// <summary>
        /// Timeout for establishing a stream connection with a remote node
        /// </summary>
        public TimeSpan TCPTimeout { get; set; }

        /// <summary>
        /// Number of nodes that will be asked to perform an indirect probe
        /// </summary>
        public int IndirectChecks { get; set; }

        /// <summary>
        /// Multiplier for the number of retransmissions that are attempted for messages
        /// </summary>
        public int RetransmitMult { get; set; }

        /// <summary>
        /// Multiplier for determining the time an inaccessible node is considered suspect
        /// </summary>
        public int SuspicionMult { get; set; }

        /// <summary>
        /// Multiplier applied to the SuspicionTimeout used as an upper bound on detection time
        /// </summary>
        public int SuspicionMaxTimeoutMult { get; set; }

        /// <summary>
        /// Interval between complete state syncs
        /// </summary>
        public TimeSpan PushPullInterval { get; set; }

        /// <summary>
        /// Interval between random node probes
        /// </summary>
        public TimeSpan ProbeInterval { get; set; }

        /// <summary>
        /// Timeout to wait for an ack from a probed node
        /// </summary>
        public TimeSpan ProbeTimeout { get; set; }

        /// <summary>
        /// Disables the fallback TCP pings
        /// </summary>
        public bool DisableTcpPings { get; set; }

        /// <summary>
        /// Controls whether to perform TCP pings on a node-by-node basis
        /// </summary>
        public Func<string, bool>? DisableTcpPingsForNode { get; set; }

        /// <summary>
        /// Increases the probe interval if the node becomes aware it might be degraded
        /// </summary>
        public int AwarenessMaxMultiplier { get; set; }

        /// <summary>
        /// Interval between sending messages that need to be gossiped
        /// </summary>
        public TimeSpan GossipInterval { get; set; }

        /// <summary>
        /// Number of random nodes to send gossip messages to per GossipInterval
        /// </summary>
        public int GossipNodes { get; set; }

        /// <summary>
        /// Interval after which a node has died that we will still try to gossip to it
        /// </summary>
        public TimeSpan GossipToTheDeadTime { get; set; }

        /// <summary>
        /// Controls whether to enforce encryption for incoming gossip
        /// </summary>
        public bool GossipVerifyIncoming { get; set; }

        /// <summary>
        /// Controls whether to enforce encryption for outgoing gossip
        /// </summary>
        public bool GossipVerifyOutgoing { get; set; }

        /// <summary>
        /// Controls message compression
        /// </summary>
        public bool EnableCompression { get; set; }

        /// <summary>
        /// Used to initialize the primary encryption key in a keyring
        /// </summary>
        public byte[] SecretKey { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Delegate for receiving and providing data to memberlist
        /// </summary>
        public IDelegate Delegate { get; set; } = null!;

        /// <summary>
        /// List of allowed CIDRs for IP validation
        /// </summary>
        public List<IPNetwork> CIDRsAllowed { get; set; } = new();

        /// <summary>
        /// Checks if an IP address is allowed based on the configured CIDRs
        /// </summary>
        public bool IsIPAllowed(IPAddress ip)
        {
            // If no CIDRs are specified, all IPs are allowed
            if (CIDRsAllowed.Count == 0)
            {
                return true;
            }

            foreach (var network in CIDRsAllowed)
            {
                if (network.Contains(ip))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Parse a list of CIDR strings into IPNetwork objects
        /// </summary>
        public static List<IPNetwork> ParseCIDRs(string[] cidrs)
        {
            var result = new List<IPNetwork>();
            if (cidrs == null) return result;

            foreach (var cidr in cidrs)
            {
                var parts = cidr.Split('/');
                if (parts.Length != 2)
                {
                    throw new ArgumentException($"Invalid CIDR format: {cidr}");
                }

                var ip = IPAddress.Parse(parts[0]);
                var prefixLength = int.Parse(parts[1]);
                result.Add(new IPNetwork(ip, prefixLength));
            }

            return result;
        }

        /// <summary>
        /// Creates a new Config with default LAN settings
        /// </summary>
        public static Config DefaultLANConfig()
        {
            return new Config
            {
                BindAddr = "0.0.0.0",
                BindPort = 7946,
                TCPTimeout = TimeSpan.FromSeconds(10),
                IndirectChecks = 3,
                RetransmitMult = 4,
                SuspicionMult = 4,
                SuspicionMaxTimeoutMult = 6,
                PushPullInterval = TimeSpan.FromSeconds(30),
                ProbeInterval = TimeSpan.FromSeconds(1),
                ProbeTimeout = TimeSpan.FromMilliseconds(500),
                AwarenessMaxMultiplier = 8,
                GossipNodes = 3,
                GossipInterval = TimeSpan.FromMilliseconds(200),
                GossipToTheDeadTime = TimeSpan.FromSeconds(30),
                EnableCompression = true
            };
        }

        /// <summary>
        /// Creates a new Config with default WAN settings
        /// </summary>
        public static Config DefaultWANConfig()
        {
            var config = DefaultLANConfig();
            config.TCPTimeout = TimeSpan.FromSeconds(30);
            config.SuspicionMult = 6;
            config.PushPullInterval = TimeSpan.FromSeconds(60);
            config.ProbeInterval = TimeSpan.FromSeconds(5);
            config.ProbeTimeout = TimeSpan.FromSeconds(3);
            return config;
        }

        /// <summary>
        /// Returns whether encryption is enabled
        /// </summary>
        public bool EncryptionEnabled() => SecretKey.Length > 0;
    }

    /// <summary>
    /// Represents an IP network with CIDR notation
    /// </summary>
    public class IPNetwork
    {
        private readonly IPAddress _networkAddress;
        private readonly int _prefixLength;
        private readonly byte[] _networkMask;

        public IPNetwork(IPAddress networkAddress, int prefixLength)
        {
            _networkAddress = networkAddress;
            _prefixLength = prefixLength;
            _networkMask = CreateNetworkMask(networkAddress.AddressFamily, prefixLength);
        }

        public bool Contains(IPAddress ip)
        {
            if (ip.AddressFamily != _networkAddress.AddressFamily)
            {
                return false;
            }

            var ipBytes = ip.GetAddressBytes();
            var networkBytes = _networkAddress.GetAddressBytes();

            for (var i = 0; i < ipBytes.Length; i++)
            {
                if ((ipBytes[i] & _networkMask[i]) != (networkBytes[i] & _networkMask[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static byte[] CreateNetworkMask(System.Net.Sockets.AddressFamily addressFamily, int prefixLength)
        {
            var maskLength = addressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? 4 : 16;
            var mask = new byte[maskLength];
            
            for (var i = 0; i < maskLength; i++)
            {
                if (prefixLength >= 8)
                {
                    mask[i] = 0xFF;
                    prefixLength -= 8;
                }
                else if (prefixLength > 0)
                {
                    mask[i] = (byte)(0xFF << (8 - prefixLength));
                    prefixLength = 0;
                }
                else
                {
                    mask[i] = 0x00;
                }
            }

            return mask;
        }
    }
}
