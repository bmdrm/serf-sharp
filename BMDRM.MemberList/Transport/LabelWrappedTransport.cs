using System;
using System.Threading.Tasks;
using System.Threading.Channels;

namespace BMDRM.MemberList.Transport
{
    /// <summary>
    /// A transport wrapper that adds label headers to all outgoing communications.
    /// </summary>
    public class LabelWrappedTransport : INodeAwareTransport
    {
        private readonly string _label;
        private readonly INodeAwareTransport _transport;

        public LabelWrappedTransport(string label, INodeAwareTransport transport)
        {
            _label = label ?? throw new ArgumentNullException(nameof(label));
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        }

        public Channel<Packet> PacketChannel => _transport.PacketChannel;

        public Channel<INetworkStream> StreamChannel => _transport.StreamChannel;

        public async Task<INetworkStream> DialAddressAsync(Address address, TimeSpan timeout)
        {
            var conn = await _transport.DialAddressAsync(address, timeout);
            try
            {
                await LabelHeaderUtils.AddLabelHeaderToStreamAsync(conn, _label);
                return conn;
            }
            catch (Exception ex)
            {
                conn.Dispose();
                throw new InvalidOperationException("Failed to add label header to stream", ex);
            }
        }

        public async Task<INetworkStream> DialAsync(string address, TimeSpan timeout)
        {
            var conn = await _transport.DialAsync(address, timeout);
            try
            {
                await LabelHeaderUtils.AddLabelHeaderToStreamAsync(conn, _label);
                return conn;
            }
            catch (Exception ex)
            {
                conn.Dispose();
                throw new InvalidOperationException("Failed to add label header to stream", ex);
            }
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
            try
            {
                var labeledBuffer = await LabelHeaderUtils.AddLabelHeaderToPacketAsync(buffer, _label);
                return await _transport.WriteToAddressAsync(labeledBuffer, address);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to add label header to packet", ex);
            }
        }

        public async Task<DateTime> WriteToAsync(byte[] buffer, string address)
        {
            try
            {
                var labeledBuffer = await LabelHeaderUtils.AddLabelHeaderToPacketAsync(buffer, _label);
                return await _transport.WriteToAsync(labeledBuffer, address);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to add label header to packet", ex);
            }
        }
    }
}
