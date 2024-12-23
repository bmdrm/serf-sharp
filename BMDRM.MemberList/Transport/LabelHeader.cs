using System.Net.Sockets;

namespace BMDRM.MemberList.Transport
{
    /// <summary>
    /// Helper class for adding label headers to packets and streams
    /// </summary>
    public static class LabelHeader
    {
        /// <summary>
        /// Maximum length of a packet or stream label.
        /// </summary>
        private const int LabelMaxSize = 255;

        /// <summary>
        /// Magic type byte (244) indicating a labeled message
        /// </summary>
        private const byte HasLabelMsg = 244;

        /// <summary>
        /// Prefixes outgoing packets with the correct header if the label is not empty.
        /// </summary>
        /// <param name="buffer">The original packet buffer</param>
        /// <param name="label">The label to add</param>
        /// <returns>A new buffer with the label header prepended</returns>
        public static Task<byte[]> AddLabelHeaderToPacketAsync(byte[] buffer, string label)
        {
            if (string.IsNullOrEmpty(label))
            {
                return Task.FromResult(buffer ?? Array.Empty<byte>());
            }

            if (label.Length > LabelMaxSize)
            {
                throw new ArgumentException($"label '{label}' is too long", nameof(label));
            }

            return Task.FromResult(MakeLabelHeader(label, buffer ?? Array.Empty<byte>()));
        }

        /// <summary>
        /// Prefixes outgoing streams with the correct header if the label is not empty.
        /// </summary>
        /// <param name="stream">The stream to write the header to</param>
        /// <param name="label">The label to add</param>
        public static async Task AddLabelHeaderToStreamAsync(Stream stream, string label)
        {
            if (string.IsNullOrEmpty(label))
            {
                return;
            }

            if (label.Length > LabelMaxSize)
            {
                throw new ArgumentException($"label '{label}' is too long", nameof(label));
            }

            var header = MakeLabelHeader(label, Array.Empty<byte>());
            await stream.WriteAsync(header);
        }

        /// <summary>
        /// Removes the label header from a packet if present and returns the label and remaining packet.
        /// </summary>
        /// <param name="buffer">The packet buffer to process</param>
        /// <returns>A tuple containing (remaining packet bytes, label string)</returns>
        /// <exception cref="InvalidOperationException">Thrown when the packet is malformed</exception>
        public static (byte[] Packet, string Label) RemoveLabelHeaderFromPacket(byte[] buffer)
        {
            if (buffer == null || buffer.Length == 0)
            {
                return (Array.Empty<byte>(), string.Empty);
            }

            // Check if this is a labeled message
            if (buffer[0] != HasLabelMsg)
            {
                return (buffer, string.Empty);
            }

            // Need at least 2 bytes for type and size
            if (buffer.Length < 2)
            {
                throw new InvalidOperationException("cannot decode label; packet has been truncated");
            }

            var labelLen = buffer[1];
            if (labelLen == 0)
            {
                throw new InvalidOperationException("label header cannot be empty when present");
            }

            // Check if we have enough bytes for the label
            if (buffer.Length < 2 + labelLen)
            {
                throw new InvalidOperationException("cannot decode label; packet has been truncated");
            }

            var label = System.Text.Encoding.UTF8.GetString(buffer, 2, labelLen);
            var remainingPacket = new byte[buffer.Length - (2 + labelLen)];
            if (remainingPacket.Length > 0)
            {
                Buffer.BlockCopy(buffer, 2 + labelLen, remainingPacket, 0, remainingPacket.Length);
            }

            return (remainingPacket, label);
        }

        /// <summary>
        /// Removes the label header from a stream if present and returns a new stream along with the label.
        /// </summary>
        /// <param name="stream">The stream to read from</param>
        /// <returns>A tuple containing (wrapped stream, label)</returns>
        /// <exception cref="InvalidOperationException">Thrown when the stream is malformed</exception>
        public static async Task<(Stream Stream, string Label)> RemoveLabelHeaderFromStreamAsync(Stream stream)
        {
            var firstByte = new byte[1];
            var bytesRead = await stream.ReadAsync(firstByte);
            if (bytesRead == 0)
            {
                return (stream, string.Empty);
            }

            // Check if this is a labeled message
            if (firstByte[0] != HasLabelMsg)
            {
                // Not a labeled message, wrap the stream to prepend the read byte
                var ms = new MemoryStream();
                await ms.WriteAsync(firstByte);
                await stream.CopyToAsync(ms);
                ms.Position = 0;
                return (ms, string.Empty);
            }

            // Read label length
            var labelLenByte = new byte[1];
            bytesRead = await stream.ReadAsync(labelLenByte);
            if (bytesRead == 0)
            {
                throw new InvalidOperationException("cannot decode label; stream has been truncated");
            }

            var labelLen = labelLenByte[0];
            if (labelLen == 0)
            {
                throw new InvalidOperationException("label header cannot be empty when present");
            }

            // Read label
            var labelBytes = new byte[labelLen];
            bytesRead = await stream.ReadAsync(labelBytes);
            if (bytesRead != labelLen)
            {
                throw new InvalidOperationException("cannot decode label; stream has been truncated");
            }

            var label = System.Text.Encoding.UTF8.GetString(labelBytes);
            
            // Create a new stream with the remaining data
            var remainingStream = new MemoryStream();
            await stream.CopyToAsync(remainingStream);
            remainingStream.Position = 0;
            return (remainingStream, label);
        }

        /// <summary>
        /// Creates a label header by combining the magic byte, label length, label content, and optional payload
        /// </summary>
        private static byte[] MakeLabelHeader(string label, byte[] rest)
        {
            // [type:byte] [size:byte] [size bytes]
            var labelBytes = System.Text.Encoding.UTF8.GetBytes(label);
            var newBuffer = new byte[2 + labelBytes.Length + (rest?.Length ?? 0)];
            
            newBuffer[0] = HasLabelMsg;
            newBuffer[1] = (byte)labelBytes.Length;
            
            Buffer.BlockCopy(labelBytes, 0, newBuffer, 2, labelBytes.Length);
            if (rest != null && rest.Length > 0)
            {
                Buffer.BlockCopy(rest, 0, newBuffer, 2 + labelBytes.Length, rest.Length);
            }
            
            return newBuffer;
        }

        /// <summary>
        /// Calculates the overhead size added by the label header
        /// </summary>
        public static int GetLabelOverhead(string label)
        {
            return string.IsNullOrEmpty(label) ? 0 : 2 + label.Length;
        }
    }
}
