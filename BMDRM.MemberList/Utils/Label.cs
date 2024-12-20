using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace BMDRM.MemberList.Utils
{
    /// <summary>
    /// Label utility class for adding and removing label headers from packets and streams.
    /// </summary>
    public static class Label
    {
        private const byte HasLabelMsg = 244;

        /// <summary>
        /// Maximum length of a packet or stream label.
        /// </summary>
        public const int LabelMaxSize = 255;

        /// <summary>
        /// Prefixes outgoing packets with the correct header if the label is not empty.
        /// </summary>
        /// <param name="buffer">Buffer to add header to</param>
        /// <param name="label">Label to add</param>
        /// <returns>Buffer with header added</returns>
        /// <exception cref="ArgumentException">If label is too long</exception>
        public static byte[] AddLabelHeaderToPacket(byte[] buffer, string label)
        {
            if (string.IsNullOrEmpty(label))
            {
                return buffer;
            }

            if (label.Length > LabelMaxSize)
            {
                throw new ArgumentException($"Label '{label}' is too long");
            }

            return MakeLabelHeader(label, buffer);
        }

        /// <summary>
        /// Removes any label header from the provided packet.
        /// </summary>
        /// <param name="buffer">Buffer to remove header from</param>
        /// <returns>Tuple of (buffer with header removed, extracted label)</returns>
        /// <exception cref="InvalidDataException">If buffer is malformed</exception>
        public static (byte[] buffer, string label) RemoveLabelHeaderFromPacket(byte[] buffer)
        {
            if (buffer.Length == 0)
            {
                return (buffer, string.Empty);
            }

            if (buffer[0] != HasLabelMsg)
            {
                return (buffer, string.Empty);
            }

            if (buffer.Length < 2)
            {
                throw new InvalidDataException("Cannot decode label; packet has been truncated");
            }

            int size = buffer[1];
            if (size < 1)
            {
                throw new InvalidDataException("Label header cannot be empty when present");
            }

            if (buffer.Length < 2 + size)
            {
                throw new InvalidDataException("Cannot decode label; packet has been truncated");
            }

            var label = Encoding.UTF8.GetString(buffer, 2, size);
            var newBuffer = new byte[buffer.Length - (2 + size)];
            Buffer.BlockCopy(buffer, 2 + size, newBuffer, 0, newBuffer.Length);

            return (newBuffer, label);
        }

        /// <summary>
        /// Prefixes outgoing streams with the correct header if the label is not empty.
        /// </summary>
        /// <param name="stream">Stream to write to</param>
        /// <param name="label">Label to add</param>
        /// <exception cref="ArgumentException">If label is too long</exception>
        public static async Task AddLabelHeaderToStreamAsync(Stream stream, string label)
        {
            if (string.IsNullOrEmpty(label))
            {
                return;
            }

            if (label.Length > LabelMaxSize)
            {
                throw new ArgumentException($"Label '{label}' is too long");
            }

            var header = MakeLabelHeader(label, null);
            await stream.WriteAsync(header);
        }

        /// <summary>
        /// Removes any label header from the beginning of the stream if present.
        /// </summary>
        /// <param name="stream">Stream to read from</param>
        /// <returns>Tuple of (stream with label removed, extracted label)</returns>
        /// <exception cref="InvalidDataException">If stream is malformed</exception>
        public static async Task<(Stream stream, string label)> RemoveLabelHeaderFromStreamAsync(Stream stream)
        {
            var reader = new StreamReader(stream, Encoding.UTF8, false, 1024, true);

            // First check for the type byte
            var buffer = new byte[1];
            var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, 1));
            if (bytesRead == 0) // EOF
            {
                return (stream, string.Empty);
            }

            if (buffer[0] != HasLabelMsg)
            {
                // No label, create a new stream with the first byte and the remaining data
                var remainingData = new byte[stream.Length - stream.Position];
                await stream.ReadAsync(remainingData);
                
                var combinedData = new byte[1 + remainingData.Length];
                combinedData[0] = buffer[0];
                Array.Copy(remainingData, 0, combinedData, 1, remainingData.Length);
                
                var combinedStream = new MemoryStream(combinedData);
                return (combinedStream, string.Empty);
            }

            // Read size byte
            bytesRead = await stream.ReadAsync(buffer.AsMemory(0, 1));
            if (bytesRead == 0)
            {
                throw new InvalidDataException("Cannot decode label; stream has been truncated");
            }

            int size = buffer[0];
            if (size < 1)
            {
                throw new InvalidDataException("Label header cannot be empty when present");
            }

            // Read label
            var labelBuffer = new byte[size];
            bytesRead = await stream.ReadAsync(labelBuffer.AsMemory(0, size));
            if (bytesRead != size)
            {
                throw new InvalidDataException("Cannot decode label; stream has been truncated");
            }

            var label = Encoding.UTF8.GetString(labelBuffer);
            return (stream, label);
        }

        /// <summary>
        /// Gets the overhead size for a label.
        /// </summary>
        /// <param name="label">Label to get overhead for</param>
        /// <returns>Size of overhead in bytes</returns>
        public static int GetLabelOverhead(string label)
        {
            if (string.IsNullOrEmpty(label))
            {
                return 0;
            }

            return 2 + Encoding.UTF8.GetByteCount(label); // type byte + size byte + label
        }

        private static byte[] MakeLabelHeader(string label, byte[]? rest)
        {
            var labelBytes = Encoding.UTF8.GetBytes(label);
            var header = new byte[2 + labelBytes.Length + (rest?.Length ?? 0)];
            header[0] = HasLabelMsg;
            header[1] = (byte)labelBytes.Length;
            Buffer.BlockCopy(labelBytes, 0, header, 2, labelBytes.Length);

            if (rest != null)
            {
                Buffer.BlockCopy(rest, 0, header, 2 + labelBytes.Length, rest.Length);
            }

            return header;
        }
    }
}
