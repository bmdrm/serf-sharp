using System.Net;
using BMDRM.MemberList.Delegates;
using System.Net.Sockets;
using System.Threading.Channels;
using BMDRM.MemberList.Transport;
using Xunit;

namespace BMDRM.MemberList.Tests.Transport
{
    public class TransportTests
    {
        private class MockNetwork
        {
            private readonly Dictionary<string, MockTransport> _transportsByAddr = new();
            private readonly Dictionary<string, MockTransport> _transportsByName = new();
            private int _port = 0;

            public MockTransport NewTransport(string name)
            {
                _port++;
                var addr = $"127.0.0.1:{_port}";
                var transport = new MockTransport(this, addr, name);
                _transportsByAddr[addr] = transport;
                _transportsByName[name] = transport;
                return transport;
            }

            internal MockTransport? GetPeer(string addr)
            {
                return _transportsByAddr.GetValueOrDefault(addr);
            }
        }

        private class MockTransport : ITransport
        {
            private readonly MockNetwork _network;
            private readonly string _addr;
            private readonly string _name;
            private readonly Channel<Packet> _packetCh;
            private readonly Channel<Socket> _streamCh;
            private bool _isShutdown;
            private readonly CancellationTokenSource _cts = new();
            private readonly Task _packetHandlerTask;
            private readonly Task _streamHandlerTask;
            private IDelegate? _delegate;

            public MockTransport(MockNetwork network, string addr, string name)
            {
                _network = network;
                _addr = addr;
                _name = name;
                _packetCh = Channel.CreateUnbounded<Packet>();
                _streamCh = Channel.CreateUnbounded<Socket>();
                _packetHandlerTask = HandlePacketsAsync();
                _streamHandlerTask = HandleStreamsAsync();
            }

            private async Task HandlePacketsAsync()
            {
                try
                {
                    await foreach (var packet in _packetCh.Reader.ReadAllAsync(_cts.Token))
                    {
                        _delegate?.NotifyMsg(packet.Buffer);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when shutting down
                }
            }

            private async Task HandleStreamsAsync()
            {
                try
                {
                    await foreach (var socket in _streamCh.Reader.ReadAllAsync(_cts.Token))
                    {
                        _ = HandleSocketAsync(socket);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when shutting down
                }
            }

            private async Task HandleSocketAsync(Socket socket)
            {
                try
                {
                    var buffer = new byte[1024];
                    var bytesRead = await socket.ReceiveAsync(buffer);
                    if (bytesRead > 0)
                    {
                        var msg = new byte[bytesRead];
                        Array.Copy(buffer, msg, bytesRead);
                        _delegate?.NotifyMsg(msg);
                    }
                }
                catch (Exception)
                {
                    // Ignore socket errors
                }
                finally
                {
                    try
                    {
                        socket.Close();
                    }
                    catch
                    {
                        // Ignore
                    }
                }
            }

            public void SetDelegate(IDelegate d)
            {
                _delegate = d;
            }

            public async Task<(IPAddress ip, int port)> FinalAdvertiseAddrAsync(string ip, int port)
            {
                var parts = _addr.Split(':');
                if (parts.Length != 2)
                {
                    throw new InvalidOperationException($"Invalid address format: {_addr}");
                }

                return (IPAddress.Parse(parts[0]), int.Parse(parts[1]));
            }

            public async Task<DateTime> WriteToAsync(byte[] buffer, string addr)
            {
                var dest = _network.GetPeer(addr);
                if (dest == null)
                {
                    throw new InvalidOperationException($"No route to {addr}");
                }

                var now = DateTime.UtcNow;
                await dest._packetCh.Writer.WriteAsync(new Packet(
                    buffer,
                    new IPEndPoint(IPAddress.Parse(_addr.Split(':')[0]), int.Parse(_addr.Split(':')[1])),
                    now));
                return now;
            }

            public async Task<Socket> DialTimeoutAsync(string addr, TimeSpan timeout)
            {
                var dest = _network.GetPeer(addr);
                if (dest == null)
                {
                    throw new InvalidOperationException($"No route to {addr}");
                }

                var (client, server) = CreateSocketPair();
                await dest._streamCh.Writer.WriteAsync(server);
                return client;
            }

            public async Task ShutdownAsync()
            {
                if (!_isShutdown)
                {
                    _isShutdown = true;
                    _cts.Cancel();
                    _packetCh.Writer.Complete();
                    _streamCh.Writer.Complete();
                    try
                    {
                        await Task.WhenAll(_packetHandlerTask, _streamHandlerTask);
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected
                    }
                }
            }

            public IAsyncEnumerable<Packet> PacketStream => _packetCh.Reader.ReadAllAsync();

            public IAsyncEnumerable<Socket> StreamStream => _streamCh.Reader.ReadAllAsync();

            private static (Socket, Socket) CreateSocketPair()
            {
                var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen(1);

                var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                client.Connect((IPEndPoint)listener.LocalEndPoint!);

                var server = listener.Accept();
                listener.Close();

                return (client, server);
            }

            public IPEndPoint GetLocalAddress()
            {
                var parts = _addr.Split(':');
                return new IPEndPoint(IPAddress.Parse(parts[0]), int.Parse(parts[1]));
            }
        }

        private class MockDelegate : IDelegate
        {
            private readonly List<byte[]> _messages = new();

            public void NotifyMsg(byte[] msg)
            {
                _messages.Add(msg);
            }

            public List<byte[]> GetMessages()
            {
                return _messages;
            }

            public byte[] NodeMeta(int limit)
            {
                return Array.Empty<byte>();
            }

            public byte[][] GetBroadcasts(int overhead, int limit)
            {
                return Array.Empty<byte[]>();
            }

            public byte[] LocalState(bool join)
            {
                return Array.Empty<byte>();
            }

            public void MergeRemoteState(byte[] buf, bool join)
            {
                // No-op for mock
            }
        }

        [Fact]
        public async Task Join_SuccessfullyJoinsCluster()
        {
            // Arrange
            var net = new MockNetwork();
            var t1 = net.NewTransport("node1");

            var c1 = new Config 
            { 
                Name = "node1",
                Transport = t1
            };
            var m1 = await Memberlist.CreateAsync(c1);
            await m1.SetAliveAsync();
            m1.Schedule();
            try
            {
                var c2 = new Config
                {
                    Name = "node2",
                    Transport = net.NewTransport("node2")
                };
                var m2 = await Memberlist.CreateAsync(c2);
                await m2.SetAliveAsync();
                m2.Schedule();
                try
                {
                    // Act
                    var (num, err) = await m2.JoinAsync(new[] { $"{c1.Name}/{t1.GetLocalAddress()}" });

                    // Assert
                    Assert.Equal(1, num);
                    Assert.Null(err);
                    Assert.Equal(2, m2.Members().Count);
                    Assert.Equal(2, m2.EstNumNodes());
                }
                finally
                {
                    await m2.ShutdownAsync();
                }
            }
            finally
            {
                await m1.ShutdownAsync();
            }
        }

        [Fact]
        public async Task Send_SuccessfullySendsMessages()
        {
            // Arrange
            var net = new MockNetwork();
            var t1 = net.NewTransport("node1");
            var d1 = new MockDelegate();

            var c1 = new Config
            {
                Name = "node1",
                Transport = t1,
                Delegate = d1
            };
            ((MockTransport)t1).SetDelegate(d1);
            var m1 = await Memberlist.CreateAsync(c1);
            await m1.SetAliveAsync();
            m1.Schedule();
            try
            {
                var c2 = new Config
                {
                    Name = "node2",
                    Transport = net.NewTransport("node2")
                };
                var m2 = await Memberlist.CreateAsync(c2);
                await m2.SetAliveAsync();
                m2.Schedule();
                try
                {
                    // Join the cluster
                    var (num, err) = await m2.JoinAsync(new[] { $"{c1.Name}/{t1.GetLocalAddress()}" });
                    Assert.Equal(1, num);
                    Assert.Null(err);

                    // Find node1
                    var n1 = m2.Members().FirstOrDefault(n => n.Name == c1.Name);
                    Assert.NotNull(n1);

                    // Act
                    await m2.SendToAsync(t1.GetLocalAddress(), "SendTo"u8.ToArray());
                    await m2.SendToUdpAsync(n1, "SendToUDP"u8.ToArray());
                    await m2.SendToTcpAsync(n1, "SendToTCP"u8.ToArray());
                    await m2.SendBestEffortAsync(n1, "SendBestEffort"u8.ToArray());
                    await m2.SendReliableAsync(n1, "SendReliable"u8.ToArray());

                    await Task.Delay(100); // Wait for messages to be processed

                    // Assert
                    var expected = new[] { "SendTo", "SendToUDP", "SendToTCP", "SendBestEffort", "SendReliable" };
                    var received = d1.GetMessages().Select(m => System.Text.Encoding.UTF8.GetString(m)).ToArray();
                    Assert.Equal(expected.OrderBy(x => x), received.OrderBy(x => x));
                }
                finally
                {
                    await m2.ShutdownAsync();
                }
            }
            finally
            {
                await m1.ShutdownAsync();
            }
        }

        [Fact]
        public async Task TcpListen_BackoffOnErrors()
        {
            // Arrange
            var numCalls = 0;
            var logger = new TestLogger(msg =>
            {
                if (msg.Contains("Error accepting TCP connection"))
                {
                    Interlocked.Increment(ref numCalls);
                }
            });

            var transport = new NetTransport(logger);

            // Create a listener that will cause AcceptTcp calls to fail
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            listener.Stop();

            var cts = new CancellationTokenSource();
            var listenTask = transport.TcpListenAsync(listener, cts.Token);

            // Act
            await Task.Delay(4000, cts.Token); // Wait 4 seconds
            cts.Cancel();

            try
            {
                await listenTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }

            // Assert
            // In 4 seconds, we expect approximately 12 loops with exponential backoff
            // Too few calls suggests minDelay not in force
            // Too many calls suggests maxDelay not in force
            Assert.InRange(numCalls, 8, 14);
        }

        private class TestLogger : ILogger
        {
            private readonly Action<string> _onLog;

            public TestLogger(Action<string> onLog)
            {
                _onLog = onLog;
            }

            public void Log(string message)
            {
                _onLog(message);
            }

            public void LogError(string message)
            {
                throw new NotImplementedException();
            }
        }
    }
}
