using System;
using System.Net;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using BMDRM.MemberList.Transport;
using BMDRM.MemberList.Delegate;
using NetworkAddress = BMDRM.MemberList.Transport.IPNetwork;

namespace BMDRM.MemberList.Utils
{
    /// <summary>
    /// Configuration for a memberlist node.
    /// </summary>
    public class Config
    {
        /// <summary>
        /// The name of this node. This must be unique in the cluster.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Transport is a hook for providing custom code to communicate with other nodes.
        /// If this is left null, then memberlist will by default make a NetTransport
        /// using BindAddr and BindPort from this structure.
        /// </summary>
        public ITransport? Transport { get; set; }

        /// <summary>
        /// Label is an optional set of bytes to include on the outside of each packet and stream.
        /// If gossip encryption is enabled and this is set it is treated as GCM authenticated data.
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// SkipInboundLabelCheck skips the check that inbound packets and gossip streams need to be label prefixed.
        /// </summary>
        public bool SkipInboundLabelCheck { get; set; }

        /// <summary>
        /// Configuration related to what address to bind to.
        /// The port is used for both UDP and TCP gossip.
        /// It is assumed other nodes are running on this port, but they do not need to.
        /// </summary>
        public string BindAddr { get; set; } = string.Empty;
        public int BindPort { get; set; }

        /// <summary>
        /// Configuration related to what address to advertise to other cluster members.
        /// Used for NAT traversal.
        /// </summary>
        public string AdvertiseAddr { get; set; } = string.Empty;
        public int AdvertisePort { get; set; }

        /// <summary>
        /// ProtocolVersion is the configured protocol version that we will speak.
        /// This must be between ProtocolVersionMin and ProtocolVersionMax.
        /// </summary>
        public byte ProtocolVersion { get; set; }

        /// <summary>
        /// TCPTimeout is the timeout for establishing a stream connection with
        /// a remote node for a full state sync, and for stream read and write operations.
        /// This is a legacy name for backwards compatibility, but should really be called
        /// StreamTimeout now that we have generalized the transport.
        /// </summary>
        public TimeSpan TCPTimeout { get; set; }

        /// <summary>
        /// IndirectChecks is the number of nodes that will be asked to perform an indirect
        /// probe of a node in the case a direct probe fails. Memberlist waits for an ack
        /// from any single indirect node, so increasing this number will increase the
        /// likelihood that an indirect probe will succeed at the expense of bandwidth.
        /// </summary>
        public int IndirectChecks { get; set; }

        /// <summary>
        /// RetransmitMult is the multiplier for the number of retransmissions that are
        /// attempted for messages broadcasted over gossip. The actual count of retransmissions
        /// is calculated using the formula:
        /// Retransmits = RetransmitMult * log(N+1)
        /// This allows the retransmits to scale properly with cluster size.
        /// </summary>
        public int RetransmitMult { get; set; }

        /// <summary>
        /// SuspicionMult is the multiplier for determining the time an inaccessible node
        /// is considered suspect before declaring it dead. The actual timeout is calculated
        /// using the formula:
        /// SuspicionTimeout = SuspicionMult * log(N+1) * ProbeInterval
        /// </summary>
        public int SuspicionMult { get; set; }

        /// <summary>
        /// SuspicionMaxTimeoutMult is the multiplier applied to the SuspicionTimeout
        /// used as an upper bound on detection time.
        /// </summary>
        public int SuspicionMaxTimeoutMult { get; set; }

        /// <summary>
        /// PushPullInterval is the interval between complete state syncs.
        /// Complete state syncs are done with a single node over TCP and are quite
        /// expensive relative to standard gossiped messages.
        /// </summary>
        public TimeSpan PushPullInterval { get; set; }

        /// <summary>
        /// ProbeInterval is the interval between random node probes.
        /// </summary>
        public TimeSpan ProbeInterval { get; set; }

        /// <summary>
        /// ProbeTimeout is the timeout to wait for an ack from a probed node
        /// before assuming it is unhealthy.
        /// </summary>
        public TimeSpan ProbeTimeout { get; set; }

        /// <summary>
        /// DisableTcpPings will turn off the fallback TCP pings that are attempted
        /// if the direct UDP ping fails.
        /// </summary>
        public bool DisableTcpPings { get; set; }

        /// <summary>
        /// DisableTcpPingsForNode is like DisableTcpPings, but lets you control
        /// whether to perform TCP pings on a node-by-node basis.
        /// </summary>
        public Func<string, bool>? DisableTcpPingsForNode { get; set; }

        /// <summary>
        /// AwarenessMaxMultiplier will increase the probe interval if the node
        /// becomes aware that it might be degraded.
        /// </summary>
        public int AwarenessMaxMultiplier { get; set; }

        /// <summary>
        /// GossipInterval is the interval between sending messages that need to be
        /// gossiped that haven't been able to piggyback on probing messages.
        /// </summary>
        public TimeSpan GossipInterval { get; set; }

        /// <summary>
        /// GossipNodes is the number of random nodes to send gossip messages to
        /// per GossipInterval.
        /// </summary>
        public int GossipNodes { get; set; }

        /// <summary>
        /// GossipToTheDeadTime is the interval after which a node has died that
        /// we will still try to gossip to it.
        /// </summary>
        public TimeSpan GossipToTheDeadTime { get; set; }

        /// <summary>
        /// GossipVerifyIncoming controls whether to enforce encryption for incoming gossip.
        /// </summary>
        public bool GossipVerifyIncoming { get; set; }

        /// <summary>
        /// GossipVerifyOutgoing controls whether to enforce encryption for outgoing gossip.
        /// </summary>
        public bool GossipVerifyOutgoing { get; set; }

        /// <summary>
        /// EnableCompression is used to control message compression.
        /// </summary>
        public bool EnableCompression { get; set; }

        /// <summary>
        /// SecretKey is used to initialize the primary encryption key in a keyring.
        /// The value should be either 16, 24, or 32 bytes to select AES-128,
        /// AES-192, or AES-256.
        /// </summary>
        public byte[]? SecretKey { get; set; }

        /// <summary>
        /// The keyring holds all of the encryption keys used internally.
        /// </summary>
        public Keyring? Keyring { get; set; }

        /// <summary>
        /// Delegate and Events are delegates for receiving and providing data to memberlist via callback mechanisms.
        /// </summary>
        public IDelegate? Delegate { get; set; }
        public byte DelegateProtocolVersion { get; set; }
        public byte DelegateProtocolMin { get; set; }
        public byte DelegateProtocolMax { get; set; }
        public IEventDelegate? Events { get; set; }
        public IConflictDelegate? Conflict { get; set; }
        public IMergeDelegate? Merge { get; set; }
        public IPingDelegate? Ping { get; set; }
        public IAliveDelegate? Alive { get; set; }

        /// <summary>
        /// DNSConfigPath points to the system's DNS config file.
        /// </summary>
        public string DNSConfigPath { get; set; } = "/etc/resolv.conf";

        /// <summary>
        /// Logger is a custom logger which you provide.
        /// </summary>
        public ILogger? Logger { get; set; }

        /// <summary>
        /// Size of Memberlist's internal channel which handles UDP messages.
        /// </summary>
        public int HandoffQueueDepth { get; set; }

        /// <summary>
        /// Maximum number of bytes that memberlist will put in a packet.
        /// </summary>
        public int UDPBufferSize { get; set; }

        /// <summary>
        /// DeadNodeReclaimTime controls the time before a dead node's name can be reclaimed.
        /// </summary>
        public TimeSpan DeadNodeReclaimTime { get; set; }

        /// <summary>
        /// RequireNodeNames controls if the name of a node is required when sending a message to that node.
        /// </summary>
        public bool RequireNodeNames { get; set; }

        /// <summary>
        /// List of networks that are allowed to communicate with the memberlist.
        /// If nil, all networks are allowed.
        /// </summary>
        public List<NetworkAddress> CIDRsAllowed { get; set; } = new();

        /// <summary>
        /// MetricLabels is a map of optional labels to apply to all metrics emitted.
        /// </summary>
        public Dictionary<string, string> MetricLabels { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// QueueCheckInterval is the interval at which we check the message queue.
        /// </summary>
        public TimeSpan QueueCheckInterval { get; set; }

        /// <summary>
        /// MsgpackUseNewTimeFormat when set to true, force the underlying msgpack codec to use the new format of time.Time.
        /// </summary>
        public bool MsgpackUseNewTimeFormat { get; set; }

        /// <summary>
        /// Returns whether encryption is enabled.
        /// </summary>
        public bool EncryptionEnabled() => SecretKey != null && SecretKey.Length > 0;

        /// <summary>
        /// Returns true if IPAllowed must be called.
        /// </summary>
        public bool IPMustBeChecked() => CIDRsAllowed.Count > 0;

        /// <summary>
        /// Checks if the given IP address is allowed according to the CIDRsAllowed configuration.
        /// If CIDRsAllowed is empty, all IPs are allowed.
        /// </summary>
        /// <param name="ip">IP address to check</param>
        /// <exception cref="Exception">Thrown when the IP is not allowed</exception>
        public void IPAllowed(IPAddress ip)
        {
            if (CIDRsAllowed.Count == 0)
                return;

            foreach (var network in CIDRsAllowed)
            {
                if (network.Contains(ip))
                    return;
            }

            throw new Exception($"IP {ip} is not in the allowed networks");
        }

        /// <summary>
        /// Parses a list of CIDR strings into IPNetwork instances.
        /// In case of error, it returns successfully parsed CIDRs and the last error found.
        /// </summary>
        public static (List<NetworkAddress> Networks, Exception? Error) ParseCIDRs(IEnumerable<string> cidrs)
        {
            var networks = new List<NetworkAddress>();
            if (cidrs == null)
                return (networks, null);

            Exception? lastError = null;
            foreach (var cidr in cidrs.Select(c => c.Trim()))
            {
                try
                {
                    var network = NetworkAddress.Parse(cidr);
                    networks.Add(network);
                }
                catch (Exception ex)
                {
                    lastError = new Exception($"Invalid CIDR: {cidr}", ex);
                }
            }

            return (networks, lastError);
        }

        /// <summary>
        /// Creates a default configuration for LAN environments.
        /// </summary>
        public static Config DefaultLANConfig()
        {
            return new Config
            {
                Name = Dns.GetHostName(),
                BindAddr = "0.0.0.0",
                BindPort = 7946,
                AdvertiseAddr = string.Empty,
                AdvertisePort = 7946,
                ProtocolVersion = 2,
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
                EnableCompression = true,
                DNSConfigPath = "/etc/resolv.conf",
                HandoffQueueDepth = 1024,
                UDPBufferSize = 1400,
                QueueCheckInterval = TimeSpan.FromSeconds(30),
                GossipVerifyIncoming = true,
                GossipVerifyOutgoing = true
            };
        }

        /// <summary>
        /// Creates a default configuration for WAN environments.
        /// </summary>
        public static Config DefaultWANConfig()
        {
            var config = DefaultLANConfig();
            config.TCPTimeout = TimeSpan.FromSeconds(30);
            config.SuspicionMult = 6;
            config.PushPullInterval = TimeSpan.FromSeconds(60);
            config.ProbeTimeout = TimeSpan.FromSeconds(3);
            config.ProbeInterval = TimeSpan.FromSeconds(5);
            return config;
        }

        /// <summary>
        /// Creates a default configuration for local environments.
        /// </summary>
        public static Config DefaultLocalConfig()
        {
            var config = DefaultLANConfig();
            config.BindAddr = "127.0.0.1";
            config.TCPTimeout = TimeSpan.FromSeconds(1);
            config.ProbeTimeout = TimeSpan.FromMilliseconds(100);
            config.ProbeInterval = TimeSpan.FromMilliseconds(100);
            config.SuspicionMult = 2;
            config.PushPullInterval = TimeSpan.FromSeconds(15);
            return config;
        }
    }
}
