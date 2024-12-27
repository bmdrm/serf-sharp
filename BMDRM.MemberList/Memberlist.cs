using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using BMDRM.MemberList.State;
using BMDRM.MemberList.Queue;
using BMDRM.MemberList.Transport;
using BMDRM.MemberList.Core.Tracking;
using BMDRM.MemberList.Suspicion;
using BMDRM.MemberList.Core.Broadcasting;
using BMDRM.MemberList.Network;
using BMDRM.MemberList.Network.Messages;
using MessagePack;

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
        private CancellationTokenSource _stopTickCts = new();
        private int _probeIndex;

        private readonly object _ackLock = new();
        private readonly Dictionary<uint, TaskCompletionSource<(bool Complete, byte[] Payload, DateTime Timestamp)>> _ackHandlers = new();

        private TransmitLimitedQueue _broadcasts = null!;
        private readonly Awareness _awareness;

        private readonly Random _random = new();
        private const int PushPullScaleThreshold = 32;

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

            // Create a queue for storing broadcasts
            _broadcasts = new TransmitLimitedQueue
            {
                RetransmitMult = _config.RetransmitMult,
                NumNodes = EstNumNodes
            };

            // Create an awareness object
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

        private async Task ProbeAsync()
        {
            // Track the number of indexes we've considered probing
            var numCheck = 0;

            while (true)
            {
                NodeState? node = null;
                bool skip = false;

                lock (_nodeLock)
                {
                    // Make sure we don't wrap around infinitely
                    if (numCheck >= _nodes.Count)
                    {
                        return;
                    }

                    // Handle the wrap around case
                    if (_probeIndex >= _nodes.Count)
                    {
                        ResetNodes();
                        _probeIndex = 0;
                        numCheck++;
                        continue;
                    }

                    // Determine if we should probe this node
                    node = _nodes[_probeIndex];
                    if (node.Name == _config.Name || node.DeadOrLeft())
                    {
                        skip = true;
                    }

                    _probeIndex++;
                }

                if (skip)
                {
                    numCheck++;
                    continue;
                }

                // Probe the specific node
                await ProbeNodeAsync(node);
                return;
            }
        }

        private void ResetNodes()
        {
            lock (_nodeLock)
            {
                // Remove dead nodes
                _nodes.RemoveAll(n => n.State == NodeStateType.Dead);

                // Shuffle the node list
                var rng = new Random();
                var n = _nodes.Count;
                while (n > 1)
                {
                    n--;
                    var k = rng.Next(n + 1);
                    (_nodes[k], _nodes[n]) = (_nodes[n], _nodes[k]);
                }
            }
        }

        private async Task ProbeNodeAsync(NodeState node)
        {
            // Scale probe interval based on health awareness
            var probeInterval = _awareness.ScaleTimeout(_config.ProbeInterval);
            if (probeInterval > _config.ProbeInterval)
            {
                // TODO: Add metrics
                // metrics.IncrCounterWithLabels([]string{"memberlist", "degraded", "probe"}, 1, m.metricLabels)
            }

            // Prepare ping message
            var (selfAddr, selfPort) = GetAdvertise();
            var ping = new Ping
            {
                SeqNo = NextSeqNo(),
                Node = node.Name,
                SourceAddr = selfAddr.GetAddressBytes(),
                SourcePort = selfPort,
                SourceNode = _config.Name
            };

            var sent = DateTime.UtcNow;
            var deadline = sent.Add(probeInterval);

            // Create completion source for acknowledgment
            var ackSource = new TaskCompletionSource<(bool Complete, byte[] Payload, DateTime Timestamp)>();
            using var cts = new CancellationTokenSource(probeInterval);
            cts.Token.Register(() => ackSource.TrySetResult((false, Array.Empty<byte>(), DateTime.UtcNow)));

            // Set up probe channels
            SetProbeChannels(ping.SeqNo, ackSource);

            // Track awareness delta
            var awarenessDelta = 0;
            try
            {
                if (node.State == NodeStateType.Alive)
                {
                    // Send ping to alive node
                    try
                    {
                        await EncodeAndSendMessageAsync(node.Address, MessageType.Ping, ping);
                    }
                    catch (Exception ex)
                    {
                        // TODO: Add proper logging
                        Console.WriteLine($"Failed to send UDP ping: {ex.Message}");
                        if (IsRemoteFailure(ex))
                        {
                            goto HANDLE_REMOTE_FAILURE;
                        }
                        return;
                    }
                }
                else
                {
                    // Send ping with suspect message for non-alive nodes
                    var messages = new List<byte[]>();
                    
                    // Add ping message
                    var pingBytes = await EncodeMessageAsync(MessageType.Ping, ping);
                    if (pingBytes != null)
                    {
                        messages.Add(pingBytes);
                    }

                    // Add suspect message
                    var suspect = new Suspect
                    {
                        Incarnation = node.Incarnation,
                        Node = node.Name,
                        From = _config.Name
                    };
                    var suspectBytes = await EncodeMessageAsync(MessageType.Suspect, suspect);
                    if (suspectBytes != null)
                    {
                        messages.Add(suspectBytes);
                    }

                    // Send compound message
                    try
                    {
                        await SendCompoundMessageAsync(node.Address, messages);
                    }
                    catch (Exception ex)
                    {
                        // TODO: Add proper logging
                        Console.WriteLine($"Failed to send UDP compound ping and suspect message to {node.Address}: {ex.Message}");
                        if (IsRemoteFailure(ex))
                        {
                            goto HANDLE_REMOTE_FAILURE;
                        }
                        return;
                    }
                }

                // At this point we've sent the ping successfully
                awarenessDelta = -1;

                // Wait for response
                var result = await ackSource.Task;
                if (result.Complete)
                {
                    if (_config.PingDelegate != null)
                    {
                        var rtt = result.Timestamp - sent;
                        _config.PingDelegate.NotifyPingComplete(node, rtt, result.Payload);
                    }
                    return;
                }

                HANDLE_REMOTE_FAILURE:
                // Get random live nodes for indirect ping
                List<NodeState> indirectNodes;
                lock (_nodeLock)
                {
                    indirectNodes = _nodes
                        .Where(n => n.Name != _config.Name &&
                                  n.Name != node.Name &&
                                  n.State == NodeStateType.Alive)
                        .OrderBy(_ => _random.Next())
                        .Take(_config.IndirectChecks)
                        .ToList();
                }

                // Attempt indirect pings
                var expectedNacks = 0;
                var indirect = new IndirectPingRequest
                {
                    SeqNo = ping.SeqNo,
                    Target = node.Address.Address.GetAddressBytes(),
                    Port = (ushort)node.Address.Port,
                    Node = node.Name,
                    SourceAddr = selfAddr.GetAddressBytes(),
                    SourcePort = selfPort,
                    SourceNode = _config.Name
                };

                foreach (var peer in indirectNodes)
                {
                    indirect.Nack = peer.PMax >= 4;
                    if (indirect.Nack)
                    {
                        expectedNacks++;
                    }

                    try
                    {
                        await EncodeAndSendMessageAsync(peer.Address, MessageType.IndirectPing, indirect);
                    }
                    catch (Exception ex)
                    {
                        // TODO: Add proper logging
                        Console.WriteLine($"Failed to send indirect ping: {ex.Message}");
                        continue;
                    }
                }

                // Update awareness delta based on expected nacks
                awarenessDelta = expectedNacks;
            }
            finally
            {
                // Apply the awareness delta
                _awareness.ApplyDelta(awarenessDelta);
            }
        }

        private bool IsRemoteFailure(Exception ex)
        {
            // TODO: Implement proper remote failure detection
            return ex is SocketException || ex is IOException;
        }

        private uint NextSeqNo()
        {
            return (uint)Interlocked.Increment(ref _sequenceNum);
        }

        private void SetProbeChannels(uint seqNo, TaskCompletionSource<(bool Complete, byte[] Payload, DateTime Timestamp)> ackSource)
        {
            // Add the handler
            lock (_ackLock)
            {
                _ackHandlers[seqNo] = ackSource;
            }

            // Create a timer to clean up the handler after timeout
            var timer = new System.Timers.Timer(_config.ProbeTimeout.TotalMilliseconds);
            timer.Elapsed += (s, e) =>
            {
                lock (_ackLock)
                {
                    if (_ackHandlers.Remove(seqNo))
                    {
                        ackSource.TrySetResult((false, Array.Empty<byte>(), DateTime.UtcNow));
                    }
                }
                timer.Dispose();
            };
            timer.AutoReset = false;
            timer.Start();
        }

        private (IPAddress Address, ushort Port) GetAdvertise()
        {
            lock (_advertiseLock)
            {
                return (_advertiseAddr, _advertisePort);
            }
        }

        private async Task EncodeAndSendMessageAsync(IPEndPoint address, MessageType type, object message)
        {
            var bytes = await EncodeMessageAsync(type, message);
            if (bytes != null)
            {
                await SendMessageAsync(address, bytes);
            }
        }

        private async Task<byte[]?> EncodeMessageAsync(MessageType type, object message)
        {
            try
            {
                return Util.Encode(type, message);
            }
            catch (Exception)
            {
                // Return null to indicate encoding failure
                return null;
            }
        }

        private async Task SendMessageAsync(IPEndPoint address, byte[] message)
        {
            // TODO: Implement UDP message sending
            await Task.CompletedTask;
        }

        private async Task SendCompoundMessageAsync(IPEndPoint address, List<byte[]> messages)
        {
            if (messages.Count == 0) return;
            
            // TODO: Implement compound message sending
            // This should concatenate messages with proper headers
            await Task.CompletedTask;
        }

        private async Task<(List<NodeState> RemoteStates, byte[] UserState)> SendAndReceiveStateAsync(IPEndPoint address, bool join)
        {
            // TODO: Implement state exchange
            return (new List<NodeState>(), Array.Empty<byte>());
        }

        private async Task MergeRemoteStateAsync(bool join, List<NodeState> remoteStates, byte[] userState)
        {
            // TODO: Implement state merging
            // This should:
            // 1. Verify protocol compatibility
            // 2. Update local node state based on remote state
            // 3. Handle user state merging through delegate if present
            await Task.CompletedTask;
        }

        private async Task GossipAsync()
        {
            // TODO: Implement gossip logic
            // This method should:
            // 1. Select random nodes for gossiping
            // 2. Send updates about other nodes' states
            // 3. Process received gossip messages
            await Task.CompletedTask;
        }

        public async Task SetAliveAsync()
        {
            // Implementation will come later
        }

        public void Schedule()
        {
            lock (_tickerLock)
            {
                // If we already have tickers, then don't do anything
                if (_tickers.Count > 0)
                    return;

                // Create a probe timer if needed
                if (_config.ProbeInterval > TimeSpan.Zero)
                {
                    var probeTimer = new System.Timers.Timer(_config.ProbeInterval.TotalMilliseconds);
                    probeTimer.Elapsed += async (sender, e) => await ProbeAsync();
                    probeTimer.Start();
                    _tickers.Add(probeTimer);
                }

                // Create a push-pull timer if needed
                if (_config.PushPullInterval > TimeSpan.Zero)
                {
                    var pushPullTimer = new System.Timers.Timer(_config.PushPullInterval.TotalMilliseconds);
                    pushPullTimer.Elapsed += async (sender, e) => await PushPullTriggerAsync();
                    pushPullTimer.Start();
                    _tickers.Add(pushPullTimer);
                }

                // Create a gossip timer if needed
                if (_config.GossipInterval <= TimeSpan.Zero || _config.GossipNodes <= 0) return;

                var gossipTimer = new System.Timers.Timer(_config.GossipInterval.TotalMilliseconds);
                gossipTimer.Elapsed += async (sender, e) => await GossipAsync();
                gossipTimer.Start();
                _tickers.Add(gossipTimer);

            }
        }

        public void Deschedule()
        {
            lock (_tickerLock)
            {
                // If we have no tickers, then we aren't scheduled
                if (_tickers.Count == 0)
                {
                    return;
                }

                // Cancel all ongoing operations
                _stopTickCts.Cancel();

                // Explicitly stop and dispose all timers
                foreach (var timer in _tickers)
                {
                    timer.Stop();
                    timer.Dispose();
                }
                _tickers.Clear();

                // Create a new CancellationTokenSource for future use
                _stopTickCts = new CancellationTokenSource();
            }
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
                var encoded = MessagePackSerializer.Serialize(msg);
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

        private async Task PushPullTriggerAsync()
        {
            var interval = _config.PushPullInterval;

            // Use a random stagger to avoid synchronizing
            var randStagger = TimeSpan.FromTicks((long)(_random.NextDouble() * interval.Ticks));
            try
            {
                await Task.Delay(randStagger, _shutdownCts.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            // Tick using a dynamic timer
            while (!_shutdownCts.Token.IsCancellationRequested)
            {
                var tickTime = PushPullScale(interval, EstNumNodes());
                try
                {
                    await Task.Delay(tickTime, _shutdownCts.Token);
                    await PushPullAsync();
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        private TimeSpan PushPullScale(TimeSpan interval, int n)
        {
            // Don't scale until we cross the threshold
            if (n <= PushPullScaleThreshold)
            {
                return interval;
            }

            var multiplier = Math.Ceiling(Math.Log2(n) - Math.Log2(PushPullScaleThreshold)) + 1.0;
            return TimeSpan.FromTicks((long)(multiplier * interval.Ticks));
        }

        private async Task PushPullAsync()
        {
            // Get a random live node
            NodeState? node;
            lock (_nodeLock)
            {
                var eligibleNodes = _nodes
                    .Where(n => n.Name != _config.Name && n.State == NodeStateType.Alive)
                    .ToList();

                if (eligibleNodes.Count == 0)
                {
                    return;
                }

                var randomIndex = _random.Next(eligibleNodes.Count);
                node = eligibleNodes[randomIndex];
            }

            // Attempt a push-pull
            try
            {
                await PushPullNodeAsync(node.Address, false);
            }
            catch (Exception ex)
            {
                // TODO: Add proper logging
                Console.WriteLine($"Push/Pull with {node.Name} failed: {ex.Message}");
            }
        }

        private async Task PushPullNodeAsync(IPEndPoint address, bool join)
        {
            try
            {
                // Attempt to send and receive state with the node
                var (remoteStates, userState) = await SendAndReceiveStateAsync(address, join);
                await MergeRemoteStateAsync(join, remoteStates, userState);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to complete push/pull with node at {address}: {ex.Message}", ex);
            }
        }
    }
}
