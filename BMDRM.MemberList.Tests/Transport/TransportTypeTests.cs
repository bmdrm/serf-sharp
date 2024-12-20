using System;
using System.Net;
using System.Threading.Channels;
using System.Threading.Tasks;
using BMDRM.MemberList.Transport;
using Xunit;

namespace BMDRM.MemberList.Tests.Transport
{
    public class TransportTypeTests
    {
        private class MockTransport : ITransport
        {
            public Channel<Packet> PacketChannel => Channel.CreateUnbounded<Packet>();

            public Channel<INetworkStream> StreamChannel => Channel.CreateUnbounded<INetworkStream>();

            public Task<INetworkStream> DialAsync(string address, TimeSpan timeout)
            {
                throw new NotImplementedException();
            }

            public Task<(IPAddress Address, int Port)> FinalAdvertiseAddressAsync(string ip, int port)
            {
                throw new NotImplementedException();
            }

            public Task ShutdownAsync()
            {
                throw new NotImplementedException();
            }

            public Task<DateTime> WriteToAsync(byte[] buffer, string address)
            {
                throw new NotImplementedException();
            }
        }

        private class MockNodeAwareTransport : INodeAwareTransport
        {
            public Channel<Packet> PacketChannel => Channel.CreateUnbounded<Packet>();

            public Channel<INetworkStream> StreamChannel => Channel.CreateUnbounded<INetworkStream>();

            public Task<INetworkStream> DialAddressAsync(Address address, TimeSpan timeout)
            {
                throw new NotImplementedException();
            }

            public Task<INetworkStream> DialAsync(string address, TimeSpan timeout)
            {
                throw new NotImplementedException();
            }

            public Task<(IPAddress Address, int Port)> FinalAdvertiseAddressAsync(string ip, int port)
            {
                throw new NotImplementedException();
            }

            public Task ShutdownAsync()
            {
                throw new NotImplementedException();
            }

            public Task<DateTime> WriteToAddressAsync(byte[] buffer, Address address)
            {
                throw new NotImplementedException();
            }

            public Task<DateTime> WriteToAsync(byte[] buffer, string address)
            {
                throw new NotImplementedException();
            }
        }

        [Fact]
        public void ShimNodeAwareTransport_Implements_INodeAwareTransport()
        {
            // Arrange
            var mockTransport = new MockTransport();
            var transport = new ShimNodeAwareTransport(mockTransport);

            // Assert
            Assert.IsAssignableFrom<INodeAwareTransport>(transport);
        }

        [Fact]
        public void LabelWrappedTransport_Implements_INodeAwareTransport()
        {
            // Arrange
            var mockTransport = new MockNodeAwareTransport();
            var transport = new LabelWrappedTransport("test", mockTransport);

            // Assert
            Assert.IsAssignableFrom<INodeAwareTransport>(transport);
        }
    }
}
