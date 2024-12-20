using System;
using System.Threading.Tasks;

namespace BMDRM.MemberList.Transport
{
    /// <summary>
    /// A wrapper that implements INodeAwareTransport by delegating to a basic ITransport implementation.
    /// </summary>
    public class ShimNodeAwareTransport : INodeAwareTransport
    {
        private readonly ITransport _transport;

        public ShimNodeAwareTransport(ITransport transport)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        }

        public System.Threading.Channels.Channel<Packet> PacketChannel => _transport.PacketChannel;

        public System.Threading.Channels.Channel<INetworkStream> StreamChannel => _transport.StreamChannel;

        public async Task<INetworkStream> DialAddressAsync(Address address, TimeSpan timeout)
        {
            return await _transport.DialAsync(address.Addr, timeout);
        }

        public async Task<INetworkStream> DialAsync(string address, TimeSpan timeout)
        {
            return await _transport.DialAsync(address, timeout);
        }

        public async Task<(System.Net.IPAddress Address, int Port)> FinalAdvertiseAddressAsync(string ip, int port)
        {
            return await _transport.FinalAdvertiseAddressAsync(ip, port);
        }

        public async Task ShutdownAsync()
        {
            await _transport.ShutdownAsync();
        }

        public async Task<DateTime> WriteToAddressAsync(byte[] buffer, Address address)
        {
            return await _transport.WriteToAsync(buffer, address.Addr);
        }

        public async Task<DateTime> WriteToAsync(byte[] buffer, string address)
        {
            return await _transport.WriteToAsync(buffer, address);
        }
    }
}
