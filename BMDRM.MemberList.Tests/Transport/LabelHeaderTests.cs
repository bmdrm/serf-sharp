using System.Net;
using System.Net.Sockets;
using BMDRM.MemberList.Transport;
using Xunit;

namespace BMDRM.MemberList.Tests.Transport
{
    public class LabelHeaderTests
    {
        private const byte PingMsg = 0;
        private const byte ErrMsg = 4;
        private const byte MaxEncryptionVersion = 1;
        private const byte HasLabelMsg = 244;

        [Theory]
        [InlineData(null, "", new byte[0])]
        [InlineData(new byte[0], "", new byte[0])]
        [InlineData(null, "foo", new byte[] { 244, 3, (byte)'f', (byte)'o', (byte)'o' })]
        [InlineData(new byte[] { 1, 2, 3 }, "foo", new byte[] { 244, 3, (byte)'f', (byte)'o', (byte)'o', 1, 2, 3 })]
        [InlineData(new byte[] { 1, 2, 3 }, "", new byte[] { 1, 2, 3 })]
        public async Task AddLabelHeaderToPacket_ValidInputs_ReturnsExpectedPacket(byte[] input, string label, byte[] expected)
        {
            // Act
            var result = await LabelHeader.AddLabelHeaderToPacketAsync(input, label);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task AddLabelHeaderToPacket_LabelMaxLength_Succeeds()
        {
            // Arrange
            var maxLabel = new string('a', 255);
            var input = new byte[] { 1, 2, 3 };

            // Act
            var result = await LabelHeader.AddLabelHeaderToPacketAsync(input, maxLabel);

            // Assert
            Assert.Equal(244, result[0]); // hasLabelMsg
            Assert.Equal(255, result[1]); // label length
            Assert.Equal(maxLabel, System.Text.Encoding.UTF8.GetString(result, 2, 255)); // label content
            Assert.Equal(new byte[] { 1, 2, 3 }, result.Skip(257).ToArray()); // original content
        }

        [Fact]
        public async Task AddLabelHeaderToPacket_LabelTooLong_ThrowsArgumentException()
        {
            // Arrange
            var tooLongLabel = new string('a', 256);
            var input = new byte[] { 1, 2, 3 };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => 
                LabelHeader.AddLabelHeaderToPacketAsync(input, tooLongLabel));
            Assert.Contains("is too long", ex.Message);
        }

        [Theory]
        [InlineData(new byte[0], "", new byte[0])]
        [InlineData(new byte[] { PingMsg, (byte)'b', (byte)'l', (byte)'a', (byte)'h' }, "", new byte[] { PingMsg, (byte)'b', (byte)'l', (byte)'a', (byte)'h' })]
        [InlineData(new byte[] { ErrMsg, (byte)'b', (byte)'l', (byte)'a', (byte)'h' }, "", new byte[] { ErrMsg, (byte)'b', (byte)'l', (byte)'a', (byte)'h' })]
        [InlineData(new byte[] { MaxEncryptionVersion, (byte)'b', (byte)'l', (byte)'a', (byte)'h' }, "", new byte[] { MaxEncryptionVersion, (byte)'b', (byte)'l', (byte)'a', (byte)'h' })]
        [InlineData(new byte[] { HasLabelMsg, 3, (byte)'a', (byte)'b', (byte)'c', PingMsg, (byte)'b', (byte)'l', (byte)'a', (byte)'h' }, "abc", new byte[] { PingMsg, (byte)'b', (byte)'l', (byte)'a', (byte)'h' })]
        [InlineData(new byte[] { HasLabelMsg, 3, (byte)'a', (byte)'b', (byte)'c', ErrMsg, (byte)'b', (byte)'l', (byte)'a', (byte)'h' }, "abc", new byte[] { ErrMsg, (byte)'b', (byte)'l', (byte)'a', (byte)'h' })]
        [InlineData(new byte[] { HasLabelMsg, 3, (byte)'a', (byte)'b', (byte)'c', MaxEncryptionVersion, (byte)'b', (byte)'l', (byte)'a', (byte)'h' }, "abc", new byte[] { MaxEncryptionVersion, (byte)'b', (byte)'l', (byte)'a', (byte)'h' })]
        public void RemoveLabelHeaderFromPacket_ValidInputs_ReturnsExpectedResults(byte[] input, string expectedLabel, byte[] expectedPacket)
        {
            // Act
            var (packet, label) = LabelHeader.RemoveLabelHeaderFromPacket(input);

            // Assert
            Assert.Equal(expectedLabel, label);
            Assert.Equal(expectedPacket, packet);
        }

        [Theory]
        [InlineData(new byte[] { HasLabelMsg, 0, (byte)'x' }, "label header cannot be empty when present")]
        [InlineData(new byte[] { HasLabelMsg }, "cannot decode label; packet has been truncated")]
        [InlineData(new byte[] { HasLabelMsg, 2, (byte)'x' }, "cannot decode label; packet has been truncated")]
        public void RemoveLabelHeaderFromPacket_InvalidInputs_ThrowsExpectedError(byte[] input, string expectedError)
        {
            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => LabelHeader.RemoveLabelHeaderFromPacket(input));
            Assert.Contains(expectedError, ex.Message);
        }

        [Theory]
        [InlineData("", null)]
        [InlineData("foo", new byte[] { HasLabelMsg, 3, (byte)'f', (byte)'o', (byte)'o' })]
        public async Task AddLabelHeaderToStream_ValidInputs_WritesExpectedData(string label, byte[] expectedData)
        {
            // Arrange
            using var ms = new MemoryStream();

            // Act
            await LabelHeader.AddLabelHeaderToStreamAsync(ms, label);

            // Assert
            if (expectedData == null)
            {
                Assert.Equal(0, ms.Length);
            }
            else
            {
                ms.Position = 0;
                var buffer = new byte[expectedData.Length];
                var bytesRead = await ms.ReadAsync(buffer);
                Assert.Equal(expectedData.Length, bytesRead);
                Assert.Equal(expectedData, buffer);
            }
        }

        [Fact]
        public async Task AddLabelHeaderToStream_LabelMaxLength_Succeeds()
        {
            // Arrange
            using var ms = new MemoryStream();
            var maxLabel = new string('a', 255);

            // Act
            await LabelHeader.AddLabelHeaderToStreamAsync(ms, maxLabel);

            // Assert
            ms.Position = 0;
            var buffer = new byte[257];  // 1 byte for type, 1 byte for length, 255 bytes for label
            var bytesRead = await ms.ReadAsync(buffer);
            Assert.Equal(257, bytesRead);
            Assert.Equal(HasLabelMsg, buffer[0]); // hasLabelMsg
            Assert.Equal(255, buffer[1]); // label length
            Assert.Equal(maxLabel, System.Text.Encoding.UTF8.GetString(buffer, 2, 255)); // label content
        }

        [Fact]
        public async Task AddLabelHeaderToStream_LabelTooLong_ThrowsArgumentException()
        {
            // Arrange
            using var ms = new MemoryStream();
            var tooLongLabel = new string('a', 256);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => 
                LabelHeader.AddLabelHeaderToStreamAsync(ms, tooLongLabel));
            Assert.Contains("is too long", ex.Message);
        }

        [Fact]
        public async Task AddLabelHeaderToStream_WithExtraData_PreservesData()
        {
            // Arrange
            using var ms = new MemoryStream();
            var label = "foo";
            var extraData = "EXTRA DATA"u8.ToArray();

            // Act
            await LabelHeader.AddLabelHeaderToStreamAsync(ms, label);
            await ms.WriteAsync(extraData);

            // Assert
            ms.Position = 0;
            var expectedHeader = new byte[] { HasLabelMsg, 3, (byte)'f', (byte)'o', (byte)'o' };
            var buffer = new byte[expectedHeader.Length + extraData.Length];
            var bytesRead = await ms.ReadAsync(buffer);

            Assert.Equal(expectedHeader.Length + extraData.Length, bytesRead);
            Assert.Equal(expectedHeader.Concat(extraData).ToArray(), buffer);
        }

        [Theory]
        [InlineData(new byte[0], "", new byte[0])]
        [InlineData(new byte[] { PingMsg, (byte)'b', (byte)'l', (byte)'a', (byte)'h' }, "", new byte[] { PingMsg, (byte)'b', (byte)'l', (byte)'a', (byte)'h' })]
        [InlineData(new byte[] { ErrMsg, (byte)'b', (byte)'l', (byte)'a', (byte)'h' }, "", new byte[] { ErrMsg, (byte)'b', (byte)'l', (byte)'a', (byte)'h' })]
        [InlineData(new byte[] { MaxEncryptionVersion, (byte)'b', (byte)'l', (byte)'a', (byte)'h' }, "", new byte[] { MaxEncryptionVersion, (byte)'b', (byte)'l', (byte)'a', (byte)'h' })]
        [InlineData(new byte[] { HasLabelMsg, 3, (byte)'a', (byte)'b', (byte)'c', PingMsg, (byte)'b', (byte)'l', (byte)'a', (byte)'h' }, "abc", new byte[] { PingMsg, (byte)'b', (byte)'l', (byte)'a', (byte)'h' })]
        [InlineData(new byte[] { HasLabelMsg, 3, (byte)'a', (byte)'b', (byte)'c', ErrMsg, (byte)'b', (byte)'l', (byte)'a', (byte)'h' }, "abc", new byte[] { ErrMsg, (byte)'b', (byte)'l', (byte)'a', (byte)'h' })]
        [InlineData(new byte[] { HasLabelMsg, 3, (byte)'a', (byte)'b', (byte)'c', MaxEncryptionVersion, (byte)'b', (byte)'l', (byte)'a', (byte)'h' }, "abc", new byte[] { MaxEncryptionVersion, (byte)'b', (byte)'l', (byte)'a', (byte)'h' })]
        public async Task RemoveLabelHeaderFromStream_ValidInputs_ReturnsExpectedResults(byte[] input, string expectedLabel, byte[] expectedData)
        {
            // Arrange
            using var ms = new MemoryStream(input);

            // Act
            var (stream, label) = await LabelHeader.RemoveLabelHeaderFromStreamAsync(ms);

            // Assert
            Assert.Equal(expectedLabel, label);
            
            var buffer = new byte[expectedData.Length];
            var bytesRead = await stream.ReadAsync(buffer);
            Assert.Equal(expectedData.Length, bytesRead);
            Assert.Equal(expectedData, buffer);
        }

        [Theory]
        [InlineData(new byte[] { HasLabelMsg, 0, (byte)'x' }, "label header cannot be empty when present")]
        [InlineData(new byte[] { HasLabelMsg }, "cannot decode label; stream has been truncated")]
        [InlineData(new byte[] { HasLabelMsg, 2, (byte)'x' }, "cannot decode label; stream has been truncated")]
        public async Task RemoveLabelHeaderFromStream_InvalidInputs_ThrowsExpectedError(byte[] input, string expectedError)
        {
            // Arrange
            using var ms = new MemoryStream(input);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => LabelHeader.RemoveLabelHeaderFromStreamAsync(ms));
            Assert.Contains(expectedError, ex.Message);
        }

        [Theory]
        [InlineData("", 0)]
        [InlineData("a", 3)]
        [InlineData("abcdefg", 9)]
        public void GetLabelOverhead_ReturnsExpectedSize(string label, int expectedOverhead)
        {
            // Act
            var overhead = LabelHeader.GetLabelOverhead(label);

            // Assert
            Assert.Equal(expectedOverhead, overhead);
        }
    }
}
