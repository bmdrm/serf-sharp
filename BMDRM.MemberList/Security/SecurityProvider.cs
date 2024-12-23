using System.Security.Cryptography;

namespace BMDRM.MemberList.Security
{
    public static class SecurityProvider
    {
        private const int VersionSize = 1;
        private const int NonceSize = 12;
        private const int TagSize = 16;
        private const int MaxPadOverhead = 16;
        private const int BlockSize = 16; // AES.BlockSize = 128 bits = 16 bytes

        /// <summary>
        /// Used to pad a byte buffer to a specific block size using the PKCS7 algorithm.
        /// "Ignores" some bytes to compensate for IV
        /// </summary>
        private static void Pkcs7Encode(MemoryStream buf, int ignore, int blockSize)
        {
            var n = (int)buf.Length - ignore;
            var more = blockSize - (n % blockSize);
            var padding = Enumerable.Repeat((byte)more, more).ToArray();
            buf.Write(padding);
        }

        /// <summary>
        /// Used to decode a buffer that has been padded
        /// </summary>
        private static byte[] Pkcs7Decode(byte[] buf, int blockSize)
        {
            if (buf.Length == 0)
            {
                throw new ArgumentException("Cannot decode a PKCS7 buffer of zero length");
            }
            var n = buf.Length;
            var last = buf[n - 1];
            n -= last;
            return buf[..n];
        }

        /// <summary>
        /// Returns the maximum possible overhead of encryption by version
        /// </summary>
        public static int EncryptOverhead(EncryptionVersion vsn)
        {
            return vsn switch
            {
                EncryptionVersion.MinVersion => 45, // Version: 1, IV: 12, Padding: 16, Tag: 16
                EncryptionVersion.MaxVersion => 29, // Version: 1, IV: 12, Tag: 16
                _ => throw new ArgumentException("Unsupported version")
            };
        }

        /// <summary>
        /// Used to compute the buffer size needed for a message of given length
        /// </summary>
        public static int EncryptedLength(EncryptionVersion vsn, int inp)
        {
            // If we are on version 1, there is no padding
            if (vsn >= EncryptionVersion.MaxVersion)
            {
                return VersionSize + NonceSize + inp + TagSize;
            }

            // Determine the padding size
            var padding = BlockSize - (inp % BlockSize);

            // Sum the extra parts to get total size
            return VersionSize + NonceSize + inp + padding + TagSize;
        }

        /// <summary>
        /// Used to encrypt a message with a given key.
        /// We make use of AES-128 in GCM mode. New byte buffer is the version,
        /// nonce, ciphertext and tag
        /// </summary>
        public static void EncryptPayload(EncryptionVersion vsn, byte[] key, byte[] msg, byte[] data, MemoryStream dst)
        {
            using var aesGcm = new AesGcm(key, TagSize);

            var offset = dst.Length;
            dst.Capacity = (int)(offset + EncryptedLength(vsn, msg.Length));

            // Write the encryption version
            dst.WriteByte((byte)vsn);

            // Add a random nonce
            var nonce = new byte[NonceSize];
            RandomNumberGenerator.Fill(nonce);
            dst.Write(nonce);
            var afterNonce = dst.Length;

            // Ensure we are correctly padded (only version 0)
            if (vsn == EncryptionVersion.MinVersion)
            {
                dst.Write(msg);
                Pkcs7Encode(dst, (int)(offset + VersionSize + NonceSize), BlockSize);
            }

            // Message source depends on the encryption version.
            // Version 0 uses padding, version 1 does not
            var src = vsn == EncryptionVersion.MinVersion
                ? dst.ToArray()[(int)(offset + VersionSize + NonceSize)..]
                : msg;

            var tag = new byte[TagSize];
            var ciphertext = new byte[src.Length];

            // Encrypt message using GCM
            aesGcm.Encrypt(nonce, src, ciphertext, tag, data);

            // Truncate the plaintext, and write the cipher text and tag
            dst.SetLength(afterNonce);
            dst.Write(ciphertext);
            dst.Write(tag);
        }

        /// <summary>
        /// Performs the actual decryption of ciphertext
        /// </summary>
        private static byte[] DecryptMessage(byte[] key, byte[] msg, byte[] data)
        {
            using var aesGcm = new AesGcm(key, TagSize);

            var nonce = msg[VersionSize..(VersionSize + NonceSize)];
            var ciphertext = msg[(VersionSize + NonceSize)..^TagSize];
            var tag = msg[^TagSize..];

            var plaintext = new byte[ciphertext.Length];
            aesGcm.Decrypt(nonce, ciphertext, tag, plaintext, data);

            return plaintext;
        }

        /// <summary>
        /// Used to decrypt a message with a given key, and verify it's contents.
        /// Any padding will be removed, and a slice to the plaintext is returned.
        /// Decryption is done IN PLACE!
        /// </summary>
        public static byte[] DecryptPayload(byte[][] keys, byte[] msg, byte[] data)
        {
            // Ensure we have at least one byte
            if (msg.Length == 0)
            {
                throw new ArgumentException("Cannot decrypt empty payload");
            }

            // Verify the version
            var vsn = (EncryptionVersion)msg[0];
            if (vsn > EncryptionVersion.MaxVersion)
            {
                throw new ArgumentException($"Unsupported encryption version {msg[0]}");
            }

            // Ensure the length is sane
            if (msg.Length < EncryptedLength(vsn, 0))
            {
                throw new ArgumentException($"Payload is too small to decrypt: {msg.Length}");
            }

            foreach (var key in keys)
            {
                try
                {
                    var plain = DecryptMessage(key, msg, data);
                    
                    // Remove the PKCS7 padding for vsn 0
                    return vsn == EncryptionVersion.MinVersion
                        ? Pkcs7Decode(plain, BlockSize)
                        : plain;
                }
                catch (CryptographicException)
                {
                    // Try next key
                }
            }

            throw new CryptographicException("Failed to decrypt payload with any key");
        }
    }
}
