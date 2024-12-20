using System;
using System.Net;
using System.Net.Sockets;

namespace BMDRM.MemberList.Utils
{
    /// <summary>
    /// Utility class for logging network-related information.
    /// </summary>
    public static class LoggingUtils
    {
        /// <summary>
        /// Creates a log string from an endpoint address.
        /// </summary>
        /// <param name="endPoint">The endpoint to log</param>
        /// <returns>A formatted log string</returns>
        public static string LogEndPoint(EndPoint? endPoint)
        {
            if (endPoint == null)
            {
                return "from=<unknown address>";
            }

            return $"from={endPoint}";
        }

        /// <summary>
        /// Creates a log string from an address string.
        /// </summary>
        /// <param name="address">The address string to log</param>
        /// <returns>A formatted log string</returns>
        public static string LogStringAddress(string? address)
        {
            if (string.IsNullOrEmpty(address))
            {
                return "from=<unknown address>";
            }

            return $"from={address}";
        }

        /// <summary>
        /// Creates a log string from a socket.
        /// </summary>
        /// <param name="socket">The socket to log</param>
        /// <returns>A formatted log string</returns>
        public static string LogSocket(Socket? socket)
        {
            if (socket == null || !socket.Connected)
            {
                return LogEndPoint(null);
            }

            try
            {
                return LogEndPoint(socket.RemoteEndPoint);
            }
            catch (SocketException)
            {
                return LogEndPoint(null);
            }
        }
    }
}
