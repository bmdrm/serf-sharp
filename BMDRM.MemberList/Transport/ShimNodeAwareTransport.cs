using System.Net.Sockets;

namespace BMDRM.MemberList.Transport
{
    /// <summary>
    /// ShimNodeAwareTransport wraps a basic ITransport to make it node-aware
    /// by ignoring the node name information
    /// </summary>
    public class ShimNodeAwareTransport : INodeAwareTransport
    {
        private readonly ITransport _transport;

        public ShimNodeAwareTransport(ITransport transport)
        {
            _transport = transport;
        }

        public IAsyncEnumerable<Packet> PacketStream => _transport.PacketStream;
        public IAsyncEnumerable<Socket> StreamStream => _transport.StreamStream;

        public Task<Socket> DialAddressTimeoutAsync(Address addr, TimeSpan timeout)
        {
            return _transport.DialTimeoutAsync(addr.Addr, timeout);
        }

        public Task<Socket> DialTimeoutAsync(string addr, TimeSpan timeout)
        {
            return _transport.DialTimeoutAsync(addr, timeout);
        }

        public Task<(System.Net.IPAddress ip, int port)> FinalAdvertiseAddrAsync(string ip, int port)
        {
            return _transport.FinalAdvertiseAddrAsync(ip, port);
        }

        public Task ShutdownAsync()
        {
            return _transport.ShutdownAsync();
        }

        public Task<DateTime> WriteToAddressAsync(byte[] buffer, Address addr)
        {
            return _transport.WriteToAsync(buffer, addr.Addr);
        }

        public Task<DateTime> WriteToAsync(byte[] buffer, string addr)
        {
            return _transport.WriteToAsync(buffer, addr);
        }
    }
}
