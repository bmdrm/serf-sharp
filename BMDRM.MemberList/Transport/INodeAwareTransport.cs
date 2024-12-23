using System.Net.Sockets;

namespace BMDRM.MemberList.Transport
{
    /// <summary>
    /// INodeAwareTransport extends ITransport with methods that are aware of node names
    /// </summary>
    public interface INodeAwareTransport : ITransport
    {
        /// <summary>
        /// WriteToAddress is like WriteTo but takes an Address instead of just a string address
        /// </summary>
        Task<DateTime> WriteToAddressAsync(byte[] buffer, Address addr);

        /// <summary>
        /// DialAddressTimeout is like DialTimeout but takes an Address instead of just a string address
        /// </summary>
        Task<Socket> DialAddressTimeoutAsync(Address addr, TimeSpan timeout);
    }
}
