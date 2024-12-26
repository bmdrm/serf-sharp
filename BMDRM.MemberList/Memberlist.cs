using System.Collections.Concurrent;
using System.Net;
using BMDRM.MemberList.State;
using BMDRM.MemberList.Queue;
using BMDRM.MemberList.Transport;
using BMDRM.MemberList.Core.Tracking;
using BMDRM.MemberList.Suspicion;
using BMDRM.MemberList.Core.Broadcasting;
using BMDRM.MemberList.Network;

namespace BMDRM.MemberList
{
    public class Memberlist
    {
        private uint _sequenceNum;  // Local sequence number
        private uint _incarnation;  // Local incarnation number
        private uint _numNodes;     // Number of known nodes (estimate)
        private uint _pushPullReq;  // Number of push/pull requests

        private readonly object _advertiseLock = new();
        private IPAddress _advertiseAddr = null!;
        private ushort _advertisePort;

        private readonly Config _config;
        private int _shutdown;  // Used as an atomic boolean value
        private readonly CancellationTokenSource _shutdownCts = new();
        private int _leave;     // Used as an atomic boolean value
        private readonly CancellationTokenSource _leaveBroadcastCts = new();

        private readonly object _shutdownLock = new();  // Serializes calls to shut down
        private readonly object _leaveLock = new();     // Serializes calls to Leave

        private readonly INodeAwareTransport _transport;

        private readonly CancellationTokenSource _handoffCts = new();
        private readonly LinkedList<object> _highPriorityMsgQueue = [];
        private readonly LinkedList<object> _lowPriorityMsgQueue = [];
        private readonly object _msgQueueLock = new();

        private readonly object _nodeLock = new();
        private readonly List<NodeState> _nodes = [];  // Known nodes
        private readonly Dictionary<string, NodeState> _nodeMap = new();  // Maps Node.Name -> NodeState
        private readonly Dictionary<string, Suspicion.Suspicion> _nodeTimers = new();  // Maps Node.Name -> suspicion timer

        private readonly object _tickerLock = new();
        private readonly List<System.Timers.Timer> _tickers = [];
        private readonly CancellationTokenSource _stopTickCts = new();
        private int _probeIndex;

        private readonly object _ackLock = new();
        private readonly Dictionary<uint, AckHandler> _ackHandlers = new();

        private TransmitLimitedQueue _broadcasts = null!;
        private readonly Awareness _awareness;

        private Memberlist(Config config)
        {
            _config = config;
            
            // Set up a network transport by default if a custom one wasn't given
            config.Transport ??= new NetTransport(new ConsoleLogger());

            // Wrap transport in label handler if needed
            if (config.Transport is INodeAwareTransport nat)
            {
                _transport = nat;
            }
            else
            {
                // Wrap in a shim to make it node-aware
                _transport = new ShimNodeAwareTransport(config.Transport);
            }

            // Initialize awareness with max multiplier
            _awareness = new Awareness(config.AwarenessMaxMultiplier, []);
        }

        public static async Task<Memberlist> CreateAsync(Config config)
        {
            var m = new Memberlist(config);
            await m.InitializeAsync();
            return m;
        }

        private async Task InitializeAsync()
        {
            // Add self to members
            _nodes.Add(new NodeState 
            { 
                Name = _config.Name,
                Address = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 0) // Will be updated later
            });
        }

        public async Task SetAliveAsync()
        {
            // Implementation will come later
        }

        public void Schedule()
        {
            // Implementation will come later
        }

        public async Task ShutdownAsync()
        {
            lock (_shutdownLock)
            {
                if (Interlocked.Exchange(ref _shutdown, 1) == 1)
                    return;

                _shutdownCts.Cancel();
            }

            await _transport.ShutdownAsync();
        }

        public List<Node> Members()
        {
            lock (_nodeLock)
            {
                return _nodes.Where(n => n.State == NodeStateType.Alive)
                            .Cast<Node>()
                            .ToList();
            }
        }

        public int NumMembers()
        {
            lock (_nodeLock)
            {
                return _nodes.Count(n => n.State == NodeStateType.Alive);
            }
        }

        public async Task<(int, Exception?)> JoinAsync(string[]? nodes)
        {
            if (nodes == null || nodes.Length == 0)
                return (0, null);

            var numSuccess = 0;
            foreach (var node in nodes)
            {
                try
                {
                    var parts = node.Split('/');
                    if (parts.Length != 2) continue;

                    var name = parts[0];
                    var addr = parts[1];

                    if (_nodes.Any(m => m.Name == name)) continue;
                    _nodes.Add(new NodeState
                    {
                        Name = name,
                        Address = ParseEndPoint(addr),
                        State = NodeStateType.Alive
                    });
                    numSuccess++;
                }
                catch (Exception)
                {
                    continue;
                }
            }

            return (numSuccess, numSuccess > 0 ? null : new Exception("Failed to join any nodes"));
        }

        private static IPEndPoint ParseEndPoint(string addr)
        {
            var parts = addr.Split(':');
            if (parts.Length != 2)
            {
                throw new ArgumentException($"Invalid endpoint format: {addr}");
            }

            return new IPEndPoint(IPAddress.Parse(parts[0]), int.Parse(parts[1]));
        }

        public async Task SendToAsync(IPEndPoint addr, byte[] msg)
        {
            await _transport.WriteToAsync(msg, addr.ToString());
        }

        public async Task SendToUdpAsync(Node node, byte[] msg)
        {
            var addr = new Address(node.Address.ToString(), node.Name);
            await _transport.WriteToAddressAsync(msg, addr);
            _config.Delegate?.NotifyMsg(msg);
        }

        public async Task SendToTcpAsync(Node node, byte[] msg)
        {
            var addr = new Address(node.Address.ToString(), node.Name);
            var socket = await _transport.DialAddressTimeoutAsync(addr, TimeSpan.FromSeconds(10));
            if (socket != null) await socket.SendAsync(msg);
            _config.Delegate?.NotifyMsg(msg);
        }

        public async Task SendBestEffortAsync(Node node, byte[] msg)
        {
            await SendToUdpAsync(node, msg);
        }

        public async Task SendReliableAsync(Node node, byte[] msg)
        {
            await SendToTcpAsync(node, msg);
        }

        private bool HasShutdown()
        {
            return _shutdown == 1;
        }

        public int EstNumNodes()
        {
            return _nodes.Count;
        }

        private bool HasLeft()
        {
            return Interlocked.CompareExchange(ref _leave, 0, 0) == 1;
        }

        private NodeStateType GetNodeState(string addr)
        {
            lock (_nodeLock)
            {
                return _nodeMap.TryGetValue(addr, out var state) ? state.State : NodeStateType.Dead;
            }
        }

        private DateTime GetNodeStateChange(string addr)
        {
            lock (_nodeLock)
            {
                return _nodeMap.TryGetValue(addr, out var state) ? state.StateChange : DateTime.MinValue;
            }
        }

        /// <summary>
        /// Returns the current health score of the node
        /// </summary>
        public int GetHealthScore()
        {
            return _awareness.GetHealthScore();
        }

        public void EncodeAndBroadcast(string node, MessageType msgType, object msg)
        {
            EncodeBroadcastNotify(node, msgType, msg, null);
        }

        public void EncodeBroadcastNotify(string node, MessageType msgType, object msg, Action? notify)
        {
            try
            {
                var encoded = Util.Encode(msgType, msg);
                QueueBroadcast(node, encoded, notify);
            }
            catch (Exception ex)
            {
                _config.Logger.LogError($"Failed to encode message for broadcast: {ex.Message}");
            }
        }

        public void QueueBroadcast(string node, byte[] msg, Action? notify = null)
        {
            var broadcast = new MemberListBroadcast(node, msg, notify);
            _broadcasts.QueueBroadcast(broadcast);
        }

        public List<byte[]> GetBroadcasts(int overhead, int limit)
        {
            // Get memberlist messages first
            var toSend = _broadcasts.GetBroadcasts(overhead, limit).ToList();

            // Check if the user has anything to broadcast
            var userDelegate = _config.Delegate;
            {
                // Determine the bytes used already
                var bytesUsed = toSend.Sum(msg => msg.Length + overhead);

                // Check space remaining for user messages
                var avail = limit - bytesUsed;
                if (avail <= overhead + Constants.UserMsgOverhead) return toSend;
                {
                    var userMsgs = userDelegate.GetBroadcasts(overhead + Constants.UserMsgOverhead, avail);

                    // Frame each user message
                    foreach (var msg in userMsgs)
                    {
                        var buf = new byte[msg.Length + 1];
                        buf[0] = (byte)MessageType.User;
                        Buffer.BlockCopy(msg, 0, buf, 1, msg.Length);
                        toSend.Add(buf);
                    }
                }
            }

            return toSend;
        }
    }
}
