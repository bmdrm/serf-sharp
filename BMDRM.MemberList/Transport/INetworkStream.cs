using System;
using System.IO;
using System.Net;

namespace BMDRM.MemberList.Transport
{
    /// <summary>
    /// Represents a network stream for two-way communication.
    /// </summary>
    public interface INetworkStream : IDisposable
    {
        /// <summary>
        /// Gets the underlying stream for reading and writing data.
        /// </summary>
        Stream Stream { get; }

        /// <summary>
        /// Gets the local endpoint of the connection.
        /// </summary>
        EndPoint LocalEndPoint { get; }

        /// <summary>
        /// Gets the remote endpoint of the connection.
        /// </summary>
        EndPoint RemoteEndPoint { get; }
    }
}
