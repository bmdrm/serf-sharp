using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using BMDRM.MemberList.Transport;
using Xunit;

namespace BMDRM.MemberList.Tests.Transport
{
    public class TransportTests
    {
        private class MockNetwork
        {
            private readonly Dictionary<string, MockTransport> _transports = new();

            public MockTransport NewTransport(string nodeName)
            {
                var transport = new MockTransport(nodeName, this);
                _transports[nodeName] = transport;
                return transport;
            }

            public async Task DeliverMessage(string from, string to, byte[] message)
            {
                if (_transports.TryGetValue(to, out var transport))
                {
                    await transport.ReceiveMessage(from, message);
                }
            }
        }

        private class MockTransport : INodeAwareTransport
        {
            private readonly string _nodeName;
            private readonly MockNetwork _network;
            private readonly Channel<Packet> _packetChannel;
            private readonly Channel<INetworkStream> _streamChannel;
            private readonly IPEndPoint _endpoint;

            public MockTransport(string nodeName, MockNetwork network)
            {
                _nodeName = nodeName;
                _network = network;
                _packetChannel = Channel.CreateUnbounded<Packet>();
                _streamChannel = Channel.CreateUnbounded<INetworkStream>();
                _endpoint = new IPEndPoint(IPAddress.Loopback, 12345); // Mock endpoint
            }

            public Channel<Packet> PacketChannel => _packetChannel;
            public Channel<INetworkStream> StreamChannel => _streamChannel;
            public IPEndPoint Endpoint => _endpoint;

            public Task<INetworkStream> DialAddressAsync(Address address, TimeSpan timeout)
            {
                var stream = new MockNetworkStream(_endpoint, new IPEndPoint(IPAddress.Loopback, 54321));
                return Task.FromResult<INetworkStream>(stream);
            }

            public Task<INetworkStream> DialAsync(string address, TimeSpan timeout)
            {
                return DialAddressAsync(new Address { Addr = address }, timeout);
            }

            public Task<(IPAddress Address, int Port)> FinalAdvertiseAddressAsync(string ip, int port)
            {
                return Task.FromResult((IPAddress.Parse(ip), port));
            }

            public Task ShutdownAsync()
            {
                return Task.CompletedTask;
            }

            public async Task<DateTime> WriteToAddressAsync(byte[] buffer, Address address)
            {
                await _network.DeliverMessage(_nodeName, address.Name, buffer);
                return DateTime.UtcNow;
            }

            public Task<DateTime> WriteToAsync(byte[] buffer, string address)
            {
                return WriteToAddressAsync(buffer, new Address { Addr = address });
            }

            public async Task ReceiveMessage(string from, byte[] message)
            {
                var packet = new Packet
                {
                    Buffer = message,
                    From = new IPEndPoint(IPAddress.Loopback, 54321),
                    Timestamp = DateTime.UtcNow
                };
                await _packetChannel.Writer.WriteAsync(packet);
            }
        }

        private class MockNetworkStream : INetworkStream
        {
            public MockNetworkStream(EndPoint local, EndPoint remote)
            {
                LocalEndPoint = local;
                RemoteEndPoint = remote;
            }

            public System.IO.Stream Stream => throw new NotImplementedException();
            public EndPoint LocalEndPoint { get; }
            public EndPoint RemoteEndPoint { get; }

            public void Dispose() { }
        }

        [Fact]
        public async Task Transport_Join_ShouldConnectTwoNodes()
        {
            // Arrange
            var network = new MockNetwork();
            var transport1 = network.NewTransport("node1");
            var transport2 = network.NewTransport("node2");

            // Act
            var result = await transport2.DialAddressAsync(
                new Address { Name = "node1", Addr = transport1.Endpoint.ToString() },
                TimeSpan.FromSeconds(1));

            // Assert
            Assert.NotNull(result);
            Assert.Equal(transport2.Endpoint, result.LocalEndPoint);
        }

        [Fact]
        public async Task Transport_Send_ShouldDeliverMessages()
        {
            // Arrange
            var network = new MockNetwork();
            var transport1 = network.NewTransport("node1");
            var transport2 = network.NewTransport("node2");
            var receivedMessages = new List<string>();
            var cts = new CancellationTokenSource();

            // Start message receiving task
            var receiveTask = Task.Run(async () =>
            {
                try
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        var packet = await transport1.PacketChannel.Reader.ReadAsync(cts.Token);
                        receivedMessages.Add(System.Text.Encoding.UTF8.GetString(packet.Buffer));
                    }
                }
                catch (OperationCanceledException) { }
            });

            // Act
            var messages = new[] { "SendTo", "SendToUDP", "SendToTCP", "SendBestEffort", "SendReliable" };
            foreach (var msg in messages)
            {
                await transport2.WriteToAddressAsync(
                    System.Text.Encoding.UTF8.GetBytes(msg),
                    new Address { Name = "node1", Addr = transport1.Endpoint.ToString() });
            }

            // Give some time for messages to be processed
            await Task.Delay(100);
            cts.Cancel();
            await receiveTask;

            // Assert
            Assert.Equal(messages.Length, receivedMessages.Count);
            Assert.Equal(messages, receivedMessages);
        }
    }
}
