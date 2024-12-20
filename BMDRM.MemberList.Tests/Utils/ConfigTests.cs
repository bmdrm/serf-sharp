using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using BMDRM.MemberList.Utils;
using BMDRM.MemberList.Transport;
using Xunit;

namespace BMDRM.MemberList.Tests.Utils
{
    public class ConfigTests
    {
        [Fact]
        public void IsValidAddressDefaults_LAN_ShouldAcceptLocalAddresses()
        {
            // Arrange
            var config = Config.DefaultLANConfig();
            var testIPs = new[]
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
            foreach (var ip in testIPs)
            {
                var ipAddress = IPAddress.Parse(ip);
                var ex = Record.Exception(() => config.IPAllowed(ipAddress));
                Assert.Null(ex);
            }
        }

        [Fact]
        public void IsValidAddressDefaults_WAN_ShouldAcceptLocalAddresses()
        {
            // Arrange
            var config = Config.DefaultWANConfig();
            var testIPs = new[]
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
            foreach (var ip in testIPs)
            {
                var ipAddress = IPAddress.Parse(ip);
                var ex = Record.Exception(() => config.IPAllowed(ipAddress));
                Assert.Null(ex);
            }
        }

        public class AddressOverrideTestCase
        {
            public string Name { get; set; } = string.Empty;
            public string[]? Allow { get; set; }
            public string[] Success { get; set; } = Array.Empty<string>();
            public string[] Fail { get; set; } = Array.Empty<string>();
        }

        public static IEnumerable<object[]> GetAddressOverrideTestCases()
        {
            yield return new object[]
            {
                new AddressOverrideTestCase
                {
                    Name = "Default, null allows all",
                    Allow = null,
                    Success = new[] { "127.0.0.5", "10.0.0.9", "192.168.1.7", "::1" },
                    Fail = Array.Empty<string>()
                }
            };

            yield return new object[]
            {
                new AddressOverrideTestCase
                {
                    Name = "Only IPv4",
                    Allow = new[] { "0.0.0.0/0" },
                    Success = new[] { "127.0.0.5", "10.0.0.9", "192.168.1.7" },
                    Fail = new[] { "fe80::38bc:4dff:fe62:b1ae", "::1" }
                }
            };

            yield return new object[]
            {
                new AddressOverrideTestCase
                {
                    Name = "Only IPv6",
                    Allow = new[] { "::0/0" },
                    Success = new[] { "fe80::38bc:4dff:fe62:b1ae", "::1" },
                    Fail = new[] { "127.0.0.5", "10.0.0.9", "192.168.1.7" }
                }
            };

            yield return new object[]
            {
                new AddressOverrideTestCase
                {
                    Name = "Only 127.0.0.0/8 and ::1",
                    Allow = new[] { "::1/128", "127.0.0.0/8" },
                    Success = new[] { "127.0.0.5", "::1" },
                    Fail = new[] { "::2", "178.250.0.187", "10.0.0.9", "192.168.1.7", "fe80::38bc:4dff:fe62:b1ae" }
                }
            };
        }

        [Theory]
        [MemberData(nameof(GetAddressOverrideTestCases))]
        public void IsValidAddressOverride_ShouldHandleAllowedNetworks(AddressOverrideTestCase testCase)
        {
            // Arrange
            var config = Config.DefaultLANConfig();
            var (networks, error) = Config.ParseCIDRs(testCase.Allow ?? Array.Empty<string>());
            Assert.Null(error); // Ensure CIDR parsing succeeded
            config.CIDRsAllowed = networks;

            // Act & Assert - Success cases
            foreach (var ip in testCase.Success)
            {
                var ipAddress = IPAddress.Parse(ip);
                var ex = Record.Exception(() => config.IPAllowed(ipAddress));
                Assert.Null(ex);
            }

            // Act & Assert - Failure cases
            foreach (var ip in testCase.Fail)
            {
                var ipAddress = IPAddress.Parse(ip);
                Assert.Throws<Exception>(() => config.IPAllowed(ipAddress));
            }
        }
    }
}
