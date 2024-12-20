using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace BMDRM.MemberList.Utils
{
    /// <summary>
    /// Keyring manages encryption keys for the memberlist protocol.
    /// </summary>
    public class Keyring
    {
        private readonly List<byte[]> _keys;
        private readonly object _lock = new object();

        public Keyring()
        {
            _keys = new List<byte[]>();
        }

        /// <summary>
        /// AddKey adds a new key to the keyring.
        /// </summary>
        public void AddKey(byte[] key)
        {
            if (key == null || key.Length == 0)
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            if (key.Length != 16 && key.Length != 24 && key.Length != 32)
                throw new ArgumentException("Key must be 16, 24, or 32 bytes (AES-128, AES-192, or AES-256)", nameof(key));

            lock (_lock)
            {
                _keys.Add((byte[])key.Clone());
            }
        }

        /// <summary>
        /// RemoveKey removes a key from the keyring.
        /// </summary>
        public void RemoveKey(byte[] key)
        {
            if (key == null || key.Length == 0)
                return;

            lock (_lock)
            {
                for (int i = _keys.Count - 1; i >= 0; i--)
                {
                    if (ByteArrayEquals(_keys[i], key))
                    {
                        _keys.RemoveAt(i);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the primary key, which is the first key in the keyring.
        /// </summary>
        public byte[]? GetPrimaryKey()
        {
            lock (_lock)
            {
                if (_keys.Count == 0)
                    return null;

                return (byte[])_keys[0].Clone();
            }
        }

        /// <summary>
        /// Encrypts a message using the primary key.
        /// </summary>
        public byte[] Encrypt(byte[] message, string? label = null)
        {
            var primaryKey = GetPrimaryKey();
            if (primaryKey == null)
                throw new InvalidOperationException("No encryption key available");

            using var aes = Aes.Create();
            aes.Key = primaryKey;
            aes.GenerateIV();

            byte[] ciphertext;
            using (var encryptor = aes.CreateEncryptor())
            {
                ciphertext = encryptor.TransformFinalBlock(message, 0, message.Length);
            }

            // Combine IV and ciphertext
            var result = new byte[aes.IV.Length + ciphertext.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(ciphertext, 0, result, aes.IV.Length, ciphertext.Length);

            return result;
        }

        /// <summary>
        /// Attempts to decrypt a message using each key in the keyring until one succeeds.
        /// </summary>
        public byte[]? Decrypt(byte[] encrypted, string? label = null)
        {
            if (encrypted == null || encrypted.Length == 0)
                return null;

            lock (_lock)
            {
                foreach (var key in _keys)
                {
                    try
                    {
                        using var aes = Aes.Create();
                        aes.Key = key;

                        // Extract IV from the encrypted message
                        var iv = new byte[aes.BlockSize / 8];
                        Buffer.BlockCopy(encrypted, 0, iv, 0, iv.Length);
                        aes.IV = iv;

                        // Extract the ciphertext
                        var ciphertext = new byte[encrypted.Length - iv.Length];
                        Buffer.BlockCopy(encrypted, iv.Length, ciphertext, 0, ciphertext.Length);

                        using var decryptor = aes.CreateDecryptor();
                        return decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
                    }
                    catch (CryptographicException)
                    {
                        // Try next key
                        continue;
                    }
                }
            }

            return null;
        }

        private static bool ByteArrayEquals(byte[] a1, byte[] a2)
        {
            if (a1 == null || a2 == null || a1.Length != a2.Length)
                return false;

            for (int i = 0; i < a1.Length; i++)
            {
                if (a1[i] != a2[i])
                    return false;
            }

            return true;
        }
    }
}
