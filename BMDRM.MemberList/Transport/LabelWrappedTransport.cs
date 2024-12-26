using System.Net.Sockets;

namespace BMDRM.MemberList.Transport
{
    /// <summary>
    /// LabelWrappedTransport wraps an INodeAwareTransport and adds label headers to all communications
    /// </summary>
    public class LabelWrappedTransport : INodeAwareTransport
    {
        private readonly string _label;
        private readonly INodeAwareTransport _transport;

        public LabelWrappedTransport(INodeAwareTransport transport, string label)
        {
            _transport = transport;
            _label = label;
        }

        public IAsyncEnumerable<Packet> PacketStream => _transport.PacketStream;
        public IAsyncEnumerable<Socket> StreamStream => _transport.StreamStream;

        public async Task<Socket?> DialAddressTimeoutAsync(Address addr, TimeSpan timeout)
        {
            var conn = await _transport.DialAddressTimeoutAsync(addr, timeout);
            if (conn == null) return conn;
            using var networkStream = new NetworkStream(conn, ownsSocket: false);
            await LabelHeader.AddLabelHeaderToStreamAsync(networkStream, _label);
            return conn;
        }

        public async Task<Socket?> DialTimeoutAsync(string addr, TimeSpan timeout)
        {
            var conn = await _transport.DialTimeoutAsync(addr, timeout);
            if (conn == null) return conn;
            using var networkStream = new NetworkStream(conn, ownsSocket: false);
            await LabelHeader.AddLabelHeaderToStreamAsync(networkStream, _label);
            return conn;
        }

        public Task<(System.Net.IPAddress ip, int port)> FinalAdvertiseAddrAsync(string ip, int port)
        {
            return _transport.FinalAdvertiseAddrAsync(ip, port);
        }

        public Task ShutdownAsync()
        {
            return _transport.ShutdownAsync();
        }

        public async Task<DateTime> WriteToAddressAsync(byte[] buffer, Address addr)
        {
            var labeledBuffer = await LabelHeader.AddLabelHeaderToPacketAsync(buffer, _label);
            return await _transport.WriteToAddressAsync(labeledBuffer, addr);
        }

        public async Task<DateTime> WriteToAsync(byte[] buffer, string addr)
        {
            var labeledBuffer = await LabelHeader.AddLabelHeaderToPacketAsync(buffer, _label);
            return await _transport.WriteToAsync(labeledBuffer, addr);
        }
    }
}
