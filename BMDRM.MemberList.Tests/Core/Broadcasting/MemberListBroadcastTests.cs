using BMDRM.MemberList.Core.Broadcasting;
using Xunit;

namespace BMDRM.MemberList.Tests.Core.Broadcasting
{
    public class MemberListBroadcastTests
    {
        [Fact]
        public void Invalidates_DifferentNodes_ReturnsFalse()
        {
            // Arrange
            var m1 = new MemberListBroadcast("test", Array.Empty<byte>());
            var m2 = new MemberListBroadcast("foo", Array.Empty<byte>());

            // Act & Assert
            Assert.False(m1.Invalidates(m2));
            Assert.False(m2.Invalidates(m1));
        }

        [Fact]
        public void Invalidates_SameNode_ReturnsTrue()
        {
            // Arrange
            var m1 = new MemberListBroadcast("test", Array.Empty<byte>());
            var m2 = new MemberListBroadcast("test", Array.Empty<byte>());

            // Act & Assert
            Assert.True(m1.Invalidates(m2));
        }

        [Fact]
        public void Message_ReturnsCorrectBytes()
        {
            // Arrange
            var testBytes = System.Text.Encoding.UTF8.GetBytes("test");
            var m1 = new MemberListBroadcast("test", testBytes);

            // Act
            var result = m1.Message();

            // Assert
            Assert.Equal(testBytes, result);
        }

        [Fact]
        public void Finished_CallsNotifyCallback()
        {
            // Arrange
            var callbackCalled = false;
            var m1 = new MemberListBroadcast("test", Array.Empty<byte>(), () => callbackCalled = true);

            // Act
            m1.Finished();

            // Assert
            Assert.True(callbackCalled);
        }

        [Fact]
        public void Finished_NoCallback_DoesNotThrow()
        {
            // Arrange
            var m1 = new MemberListBroadcast("test", Array.Empty<byte>());

            // Act & Assert
            var exception = Record.Exception(() => m1.Finished());
            Assert.Null(exception);
        }

        [Fact]
        public void Name_ReturnsNodeName()
        {
            // Arrange
            var nodeName = "test";
            var m1 = new MemberListBroadcast(nodeName, Array.Empty<byte>());

            // Act & Assert
            Assert.Equal(nodeName, m1.Name);
        }
    }
}
