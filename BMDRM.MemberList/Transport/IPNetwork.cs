using System;
using System.Net;
using System.Net.Sockets;

namespace BMDRM.MemberList.Transport
{
    /// <summary>
    /// Represents an IP network with CIDR notation.
    /// </summary>
    public class IPNetwork
    {
        private readonly byte[] _networkAddress;
        private readonly byte[] _networkMask;
        private readonly AddressFamily _addressFamily;

        /// <summary>
        /// Gets the network address.
        /// </summary>
        public IPAddress NetworkAddress { get; }

        /// <summary>
        /// Gets the network prefix length (CIDR notation).
        /// </summary>
        public int PrefixLength { get; }

        /// <summary>
        /// Creates a new IPNetwork instance from an IP address and prefix length.
        /// </summary>
        /// <param name="networkAddress">The network address</param>
        /// <param name="prefixLength">The prefix length in CIDR notation</param>
        public IPNetwork(IPAddress networkAddress, int prefixLength)
        {
            if (networkAddress == null)
                throw new ArgumentNullException(nameof(networkAddress));

            if (prefixLength < 0 || 
                (networkAddress.AddressFamily == AddressFamily.InterNetwork && prefixLength > 32) ||
                (networkAddress.AddressFamily == AddressFamily.InterNetworkV6 && prefixLength > 128))
            {
                throw new ArgumentException("Invalid prefix length", nameof(prefixLength));
            }

            NetworkAddress = networkAddress;
            PrefixLength = prefixLength;
            _addressFamily = networkAddress.AddressFamily;

            // Convert address to bytes
            _networkAddress = networkAddress.GetAddressBytes();

            // Create network mask
            _networkMask = CreateNetworkMask(prefixLength, _addressFamily);

            // Apply network mask to ensure network address is properly masked
            for (int i = 0; i < _networkAddress.Length; i++)
            {
                _networkAddress[i] &= _networkMask[i];
            }
        }

        /// <summary>
        /// Checks if the given IP address is contained within this network.
        /// </summary>
        /// <param name="address">The IP address to check</param>
        /// <returns>True if the address is contained within this network</returns>
        public bool Contains(IPAddress address)
        {
            if (address == null)
                return false;

            if (address.AddressFamily != _addressFamily)
                return false;

            byte[] addressBytes = address.GetAddressBytes();

            // Apply network mask to the address being checked
            for (int i = 0; i < addressBytes.Length; i++)
            {
                if ((addressBytes[i] & _networkMask[i]) != _networkAddress[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Parses a CIDR string into an IPNetwork instance.
        /// </summary>
        /// <param name="cidr">The CIDR string (e.g., "192.168.1.0/24" or "2001:db8::/32")</param>
        /// <returns>A new IPNetwork instance</returns>
        public static IPNetwork Parse(string cidr)
        {
            if (string.IsNullOrWhiteSpace(cidr))
                throw new ArgumentException("CIDR string cannot be null or empty", nameof(cidr));

            string[] parts = cidr.Split('/');
            if (parts.Length != 2)
                throw new FormatException("Invalid CIDR format. Expected format: address/prefix");

            if (!IPAddress.TryParse(parts[0].Trim(), out IPAddress address))
                throw new FormatException("Invalid IP address in CIDR");

            if (!int.TryParse(parts[1].Trim(), out int prefixLength))
                throw new FormatException("Invalid prefix length in CIDR");

            return new IPNetwork(address, prefixLength);
        }

        /// <summary>
        /// Tries to parse a CIDR string into an IPNetwork instance.
        /// </summary>
        /// <param name="cidr">The CIDR string to parse</param>
        /// <param name="network">The resulting IPNetwork instance if parsing succeeds</param>
        /// <returns>True if parsing succeeds, false otherwise</returns>
        public static bool TryParse(string cidr, out IPNetwork? network)
        {
            try
            {
                network = Parse(cidr);
                return true;
            }
            catch
            {
                network = null;
                return false;
            }
        }

        private static byte[] CreateNetworkMask(int prefixLength, AddressFamily addressFamily)
        {
            int length = addressFamily == AddressFamily.InterNetwork ? 4 : 16;
            byte[] mask = new byte[length];

            // Calculate the number of full bytes
            int fullBytes = prefixLength / 8;
            for (int i = 0; i < fullBytes && i < length; i++)
            {
                mask[i] = 0xFF;
            }

            // Calculate the remaining bits
            if (fullBytes < length)
            {
                int remainingBits = prefixLength % 8;
                if (remainingBits > 0)
                {
                    mask[fullBytes] = (byte)(0xFF << (8 - remainingBits));
                }
            }

            return mask;
        }

        /// <summary>
        /// Returns a string representation of the network in CIDR notation.
        /// </summary>
        public override string ToString()
        {
            return $"{NetworkAddress}/{PrefixLength}";
        }
    }
}
