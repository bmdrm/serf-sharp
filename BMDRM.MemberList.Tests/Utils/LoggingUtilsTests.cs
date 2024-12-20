using System.Net;
using System.Net.Sockets;
using BMDRM.MemberList.Utils;
using Xunit;

namespace BMDRM.MemberList.Tests.Utils
{
    public class LoggingUtilsTests
    {
        [Fact]
        public void LogEndPoint_NullEndPoint_ReturnsUnknownAddress()
        {
            var result = LoggingUtils.LogEndPoint(null);
            Assert.Equal("from=<unknown address>", result);
        }

        [Fact]
        public void LogEndPoint_ValidEndPoint_ReturnsFormattedString()
        {
            var endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8000);
            var result = LoggingUtils.LogEndPoint(endPoint);
            Assert.Equal("from=127.0.0.1:8000", result);
        }

        [Fact]
        public void LogStringAddress_NullAddress_ReturnsUnknownAddress()
        {
            var result = LoggingUtils.LogStringAddress(null);
            Assert.Equal("from=<unknown address>", result);
        }

        [Fact]
        public void LogStringAddress_EmptyAddress_ReturnsUnknownAddress()
        {
            var result = LoggingUtils.LogStringAddress("");
            Assert.Equal("from=<unknown address>", result);
        }

        [Fact]
        public void LogStringAddress_ValidAddress_ReturnsFormattedString()
        {
            var result = LoggingUtils.LogStringAddress("127.0.0.1:8000");
            Assert.Equal("from=127.0.0.1:8000", result);
        }

        [Fact]
        public void LogSocket_NullSocket_ReturnsUnknownAddress()
        {
            var result = LoggingUtils.LogSocket(null);
            Assert.Equal("from=<unknown address>", result);
        }

        [Fact]
        public void LogSocket_DisconnectedSocket_ReturnsUnknownAddress()
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var result = LoggingUtils.LogSocket(socket);
            Assert.Equal("from=<unknown address>", result);
        }

        [Fact]
        public void LogSocket_ConnectedSocket_ReturnsFormattedString()
        {
            // Create a listener socket
            using var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var endPoint = new IPEndPoint(IPAddress.Loopback, 0);
            listener.Bind(endPoint);
            listener.Listen(1);

            // Create a client socket and connect
            using var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            client.Connect(listener.LocalEndPoint!);

            // Accept the connection
            using var server = listener.Accept();

            // Test both sides of the connection
            var clientResult = LoggingUtils.LogSocket(client);
            var serverResult = LoggingUtils.LogSocket(server);

            Assert.Contains("from=127.0.0.1:", clientResult);
            Assert.Contains("from=127.0.0.1:", serverResult);
        }
    }
}
