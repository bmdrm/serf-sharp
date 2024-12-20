using System;
using System.Threading.Tasks;

namespace BMDRM.MemberList.Transport
{
    /// <summary>
    /// Extends ITransport with node-aware capabilities.
    /// </summary>
    public interface INodeAwareTransport : ITransport
    {
        /// <summary>
        /// Writes to a specific node address.
        /// </summary>
        /// <param name="buffer">The data to send</param>
        /// <param name="address">The target address with node information</param>
        /// <returns>Timestamp when the packet was transmitted</returns>
        Task<DateTime> WriteToAddressAsync(byte[] buffer, Address address);

        /// <summary>
        /// Creates a connection to a specific node.
        /// </summary>
        /// <param name="address">The target address with node information</param>
        /// <param name="timeout">Connection timeout duration</param>
        Task<INetworkStream> DialAddressAsync(Address address, TimeSpan timeout);
    }
}
