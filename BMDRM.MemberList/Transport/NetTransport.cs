using System.Net;
using System.Net.Sockets;

namespace BMDRM.MemberList.Transport
{
    public class NetTransport : ITransport
    {
        private readonly ILogger _logger;
        private bool _isShutdown;

        public NetTransport(ILogger logger)
        {
            _logger = logger;
        }

        public Task<(IPAddress ip, int port)> FinalAdvertiseAddrAsync(string ip, int port)
        {
            throw new NotImplementedException();
        }

        public Task<DateTime> WriteToAsync(byte[] buffer, string addr)
        {
            throw new NotImplementedException();
        }

        public Task<Socket> DialTimeoutAsync(string addr, TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        public Task ShutdownAsync()
        {
            _isShutdown = true;
            return Task.CompletedTask;
        }

        public IAsyncEnumerable<Packet> PacketStream => throw new NotImplementedException();

        public IAsyncEnumerable<Socket> StreamStream => throw new NotImplementedException();

        public async Task TcpListenAsync(TcpListener listener, CancellationToken cancellationToken)
        {
            var minDelay = TimeSpan.FromMilliseconds(5);
            var maxDelay = TimeSpan.FromSeconds(1);
            var delay = minDelay;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await listener.AcceptSocketAsync(cancellationToken);
                    delay = minDelay;
                }
                catch (Exception ex)
                {
                    _logger.Log($"Error accepting TCP connection: {ex.Message}");
                    await Task.Delay(delay, cancellationToken);
                    delay = TimeSpan.FromTicks(Math.Min(delay.Ticks * 2, maxDelay.Ticks));
                }
            }
        }
    }
}
