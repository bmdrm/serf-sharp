using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using BMDRM.MemberList.Utils;
using Xunit;

namespace BMDRM.MemberList.Tests.Utils
{
    public class LabelTests
    {
        private const byte HasLabelMsg = 244;

        [Fact]
        public void AddLabelHeaderToPacket_EmptyLabel_ReturnsOriginalBuffer()
        {
            // Arrange
            var buffer = new byte[] { 1, 2, 3 };

            // Act
            var result = Label.AddLabelHeaderToPacket(buffer, string.Empty);

            // Assert
            Assert.Same(buffer, result);
        }

        [Fact]
        public void AddLabelHeaderToPacket_LabelTooLong_ThrowsException()
        {
            // Arrange
            var buffer = new byte[] { 1, 2, 3 };
            var label = new string('x', Label.LabelMaxSize + 1);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => Label.AddLabelHeaderToPacket(buffer, label));
        }

        [Fact]
        public void AddLabelHeaderToPacket_ValidLabel_AddsHeader()
        {
            // Arrange
            var buffer = new byte[] { 1, 2, 3 };
            var label = "test";

            // Act
            var result = Label.AddLabelHeaderToPacket(buffer, label);

            // Assert
            Assert.Equal(HasLabelMsg, result[0]); // magic byte
            Assert.Equal(4, result[1]); // label length
            Assert.Equal("test", Encoding.UTF8.GetString(result, 2, 4)); // label
            Assert.Equal(1, result[6]); // original data
            Assert.Equal(2, result[7]);
            Assert.Equal(3, result[8]);
        }

        [Fact]
        public void RemoveLabelHeaderFromPacket_EmptyBuffer_ReturnsEmpty()
        {
            // Arrange
            var buffer = Array.Empty<byte>();

            // Act
            var (newBuffer, label) = Label.RemoveLabelHeaderFromPacket(buffer);

            // Assert
            Assert.Same(buffer, newBuffer);
            Assert.Empty(label);
        }

        [Fact]
        public void RemoveLabelHeaderFromPacket_NoLabel_ReturnsOriginal()
        {
            // Arrange
            var buffer = new byte[] { 1, 2, 3 };

            // Act
            var (newBuffer, label) = Label.RemoveLabelHeaderFromPacket(buffer);

            // Assert
            Assert.Same(buffer, newBuffer);
            Assert.Empty(label);
        }

        [Fact]
        public void RemoveLabelHeaderFromPacket_TruncatedPacket_ThrowsException()
        {
            // Arrange
            var buffer = new byte[] { HasLabelMsg };

            // Act & Assert
            Assert.Throws<InvalidDataException>(() => Label.RemoveLabelHeaderFromPacket(buffer));
        }

        [Fact]
        public void RemoveLabelHeaderFromPacket_ValidLabel_ExtractsLabel()
        {
            // Arrange
            var label = "test";
            var data = new byte[] { 1, 2, 3 };
            var buffer = Label.AddLabelHeaderToPacket(data, label);

            // Act
            var (newBuffer, extractedLabel) = Label.RemoveLabelHeaderFromPacket(buffer);

            // Assert
            Assert.Equal(label, extractedLabel);
            Assert.Equal(data, newBuffer);
        }

        [Fact]
        public async Task AddLabelHeaderToStream_EmptyLabel_WritesNothing()
        {
            // Arrange
            using var stream = new MemoryStream();

            // Act
            await Label.AddLabelHeaderToStreamAsync(stream, string.Empty);

            // Assert
            Assert.Equal(0, stream.Length);
        }

        [Fact]
        public async Task AddLabelHeaderToStream_ValidLabel_WritesHeader()
        {
            // Arrange
            using var stream = new MemoryStream();
            var label = "test";

            // Act
            await Label.AddLabelHeaderToStreamAsync(stream, label);

            // Assert
            stream.Position = 0;
            var written = stream.ToArray();
            Assert.Equal(HasLabelMsg, written[0]);
            Assert.Equal(4, written[1]);
            Assert.Equal("test", Encoding.UTF8.GetString(written, 2, 4));
        }

        [Fact]
        public async Task RemoveLabelHeaderFromStream_NoLabel_ReturnsEmpty()
        {
            // Arrange
            var data = new byte[] { 1, 2, 3 };
            using var stream = new MemoryStream(data);

            // Act
            var (resultStream, label) = await Label.RemoveLabelHeaderFromStreamAsync(stream);

            // Assert
            Assert.Empty(label);
            var buffer = new byte[3];
            await resultStream.ReadAsync(buffer);
            Assert.Equal(new byte[] { 1, 2, 3 }, buffer);
        }

        [Fact]
        public async Task RemoveLabelHeaderFromStream_ValidLabel_ExtractsLabel()
        {
            // Arrange
            var label = "test";
            var data = new byte[] { 1, 2, 3 };
            var buffer = Label.AddLabelHeaderToPacket(data, label);
            using var stream = new MemoryStream(buffer);

            // Act
            var (resultStream, extractedLabel) = await Label.RemoveLabelHeaderFromStreamAsync(stream);

            // Assert
            Assert.Equal(label, extractedLabel);
            var readBuffer = new byte[3];
            await resultStream.ReadAsync(readBuffer);
            Assert.Equal(data, readBuffer);
        }

        [Fact]
        public void GetLabelOverhead_EmptyLabel_ReturnsZero()
        {
            Assert.Equal(0, Label.GetLabelOverhead(string.Empty));
        }

        [Fact]
        public void GetLabelOverhead_ValidLabel_ReturnsCorrectSize()
        {
            var label = "test";
            Assert.Equal(6, Label.GetLabelOverhead(label)); // 2 bytes header + 4 bytes label
        }
    }

    /// <summary>
    /// Mock implementation of NetworkStream for testing
    /// </summary>
    internal class MockStream : NetworkStream
    {
        private readonly MemoryStream _memoryStream;
        private readonly Socket _socket;

        public MockStream(MemoryStream memoryStream)
            : base(CreateConnectedSocket())
        {
            _memoryStream = memoryStream;
            _socket = Socket;
        }

        private static Socket CreateConnectedSocket()
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var endPoint = new IPEndPoint(IPAddress.Loopback, 0);
            socket.Bind(endPoint);
            return socket;
        }

        public override bool CanRead => _memoryStream.CanRead;
        public override bool CanWrite => _memoryStream.CanWrite;
        public override bool CanSeek => _memoryStream.CanSeek;

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _memoryStream.Read(buffer, offset, count);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken)
        {
            return _memoryStream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _memoryStream.Write(buffer, offset, count);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken)
        {
            return _memoryStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, System.Threading.CancellationToken cancellationToken = default)
        {
            return _memoryStream.ReadAsync(buffer, cancellationToken);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, System.Threading.CancellationToken cancellationToken = default)
        {
            return _memoryStream.WriteAsync(buffer, cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _memoryStream.Dispose();
                _socket.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
