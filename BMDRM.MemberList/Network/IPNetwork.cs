using System.Net;

namespace BMDRM.MemberList.Network;

/// <summary>
/// Represents an IP network with CIDR notation
/// </summary>
public class IPNetwork
{
    private readonly IPAddress _networkAddress;
    private readonly int _prefixLength;
    private readonly byte[] _networkMask;

    public IPNetwork(IPAddress networkAddress, int prefixLength)
    {
        _networkAddress = networkAddress;
        _prefixLength = prefixLength;
        _networkMask = CreateNetworkMask(networkAddress.AddressFamily, prefixLength);
    }

    public bool Contains(IPAddress ip)
    {
        if (ip.AddressFamily != _networkAddress.AddressFamily)
        {
            return false;
        }

        var ipBytes = ip.GetAddressBytes();
        var networkBytes = _networkAddress.GetAddressBytes();

        return !ipBytes.Where(
            (t, i) => (t & _networkMask[i]) != (networkBytes[i] & _networkMask[i])).Any();
    }

    private static byte[] CreateNetworkMask(System.Net.Sockets.AddressFamily addressFamily, int prefixLength)
    {
        var maskLength = addressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? 4 : 16;
        var mask = new byte[maskLength];
            
        for (var i = 0; i < maskLength; i++)
        {
            switch (prefixLength)
            {
                case >= 8:
                    mask[i] = 0xFF;
                    prefixLength -= 8;
                    break;
                case > 0:
                    mask[i] = (byte)(0xFF << (8 - prefixLength));
                    prefixLength = 0;
                    break;
                default:
                    mask[i] = 0x00;
                    break;
            }
        }

        return mask;
    }
}