using System.Net;
using Xunit;

namespace BMDRM.MemberList.Tests
{
    public class ConfigTests
    {
        [Fact]
        public void IsValidAddressDefaults_LAN()
        {
            // Arrange
            var config = Config.DefaultLANConfig();
            var validAddresses = new[]
            {
                "127.0.0.1",
                "127.0.0.5",
                "10.0.0.9",
                "172.16.0.7",
                "192.168.2.1",
                "fe80::aede:48ff:fe00:1122",
                "::1"
            };

            // Act & Assert
            foreach (var ip in validAddresses)
            {
                var ipAddress = IPAddress.Parse(ip);
                Assert.True(config.IsIPAllowed(ipAddress), $"IP {ip} should be accepted for LAN");
            }
        }

        [Fact]
        public void IsValidAddressDefaults_WAN()
        {
            // Arrange
            var config = Config.DefaultWANConfig();
            var validAddresses = new[]
            {
                "127.0.0.1",
                "127.0.0.5",
                "10.0.0.9",
                "172.16.0.7",
                "192.168.2.1",
                "fe80::aede:48ff:fe00:1122",
                "::1"
            };

            // Act & Assert
            foreach (var ip in validAddresses)
            {
                var ipAddress = IPAddress.Parse(ip);
                Assert.True(config.IsIPAllowed(ipAddress), $"IP {ip} should be accepted for WAN");
            }
        }

        [Theory]
        [InlineData("Default, nil allows all", null, new[] { "127.0.0.5", "10.0.0.9", "192.168.1.7", "::1" }, new string[] { })]
        [InlineData("Only IPv4", new[] { "0.0.0.0/0" }, new[] { "127.0.0.5", "10.0.0.9", "192.168.1.7" }, new[] { "fe80::38bc:4dff:fe62:b1ae", "::1" })]
        [InlineData("Only IPv6", new[] { "::0/0" }, new[] { "fe80::38bc:4dff:fe62:b1ae", "::1" }, new[] { "127.0.0.5", "10.0.0.9", "192.168.1.7" })]
        [InlineData("Only 127.0.0.0/8 and ::1", new[] { "::1/128", "127.0.0.0/8" }, new[] { "127.0.0.5", "::1" }, new[] { "::2", "178.250.0.187", "10.0.0.9", "192.168.1.7", "fe80::38bc:4dff:fe62:b1ae" })]
        public void IsValidAddressOverride(string name, string[] allowedCIDRs, string[] successIPs, string[] failIPs)
        {
            // Arrange
            var config = Config.DefaultLANConfig();
            if (allowedCIDRs != null)
            {
                config.CIDRsAllowed = Config.ParseCIDRs(allowedCIDRs);
            }

            // Act & Assert - Success cases
            foreach (var ip in successIPs)
            {
                var ipAddress = IPAddress.Parse(ip);
                Assert.True(config.IsIPAllowed(ipAddress), $"Test case '{name}' with IP {ip} should pass");
            }

            // Act & Assert - Failure cases
            foreach (var ip in failIPs)
            {
                var ipAddress = IPAddress.Parse(ip);
                Assert.False(config.IsIPAllowed(ipAddress), $"Test case '{name}' with IP {ip} should fail");
            }
        }

        [Fact]
        public void EncryptionEnabled_WithEmptyKey_ReturnsFalse()
        {
            // Arrange
            var config = Config.DefaultLANConfig();
            config.SecretKey = Array.Empty<byte>();

            // Act & Assert
            Assert.False(config.EncryptionEnabled());
        }

        [Fact]
        public void EncryptionEnabled_WithKey_ReturnsTrue()
        {
            // Arrange
            var config = Config.DefaultLANConfig();
            config.SecretKey = new byte[] { 1, 2, 3, 4 };

            // Act & Assert
            Assert.True(config.EncryptionEnabled());
        }
    }
}
