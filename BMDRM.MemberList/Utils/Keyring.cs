using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;

namespace BMDRM.MemberList.Utils
{
    /// <summary>
    /// Keyring stores and manages encryption keys used by memberlist.
    /// </summary>
    public class Keyring
    {
        private readonly object _lock = new();
        private List<byte[]> _keys;

        /// <summary>
        /// Creates a new keyring with optional keys and primary key.
        /// </summary>
        /// <param name="keys">Optional secondary keys</param>
        /// <param name="primaryKey">Primary key for encryption</param>
        /// <exception cref="ArgumentException">If primary key is invalid or empty when keys are provided</exception>
        public Keyring(IEnumerable<byte[]>? keys = null, byte[]? primaryKey = null)
        {
            _keys = new List<byte[]>();

            if (keys?.Any() == true || primaryKey != null)
            {
                if (primaryKey == null || primaryKey.Length == 0)
                {
                    throw new ArgumentException("Empty primary key not allowed");
                }

                AddKey(primaryKey);
                if (keys != null)
                {
                    foreach (var key in keys)
                    {
                        AddKey(key);
                    }
                }
            }
        }

        /// <summary>
        /// Validates that a key is the correct size for AES encryption.
        /// </summary>
        /// <param name="key">Key to validate</param>
        /// <exception cref="ArgumentException">If key size is invalid</exception>
        public static void ValidateKey(byte[] key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (key.Length != 16 && key.Length != 24 && key.Length != 32)
            {
                throw new ArgumentException("Key size must be 16, 24 or 32 bytes");
            }
        }

        /// <summary>
        /// Adds a new key to the keyring.
        /// </summary>
        /// <param name="key">Key to add</param>
        /// <exception cref="ArgumentException">If key is invalid</exception>
        public void AddKey(byte[] key)
        {
            ValidateKey(key);

            lock (_lock)
            {
                // No-op if key is already installed
                if (_keys.Any(k => k.SequenceEqual(key)))
                    return;

                var keys = new List<byte[]>(_keys) { key };
                var primaryKey = GetPrimaryKey() ?? key;
                InstallKeys(keys, primaryKey);
            }
        }

        /// <summary>
        /// Changes the primary key used for encryption.
        /// </summary>
        /// <param name="key">Key to use as primary</param>
        /// <exception cref="Exception">If key is not in the keyring</exception>
        public void UseKey(byte[] key)
        {
            lock (_lock)
            {
                if (!_keys.Any(k => k.SequenceEqual(key)))
                {
                    throw new Exception("Requested key is not in the keyring");
                }

                InstallKeys(_keys, key);
            }
        }

        /// <summary>
        /// Removes a key from the keyring.
        /// </summary>
        /// <param name="key">Key to remove</param>
        /// <exception cref="Exception">If attempting to remove the primary key</exception>
        public void RemoveKey(byte[] key)
        {
            lock (_lock)
            {
                if (_keys.Count == 0)
                    throw new Exception("Keyring is empty");

                for (int i = 0; i < _keys.Count; i++)
                {
                    if (_keys[i].SequenceEqual(key))
                    {
                        if (i == 0)
                        {
                            throw new Exception("Removing the primary key is not allowed");
                        }

                        var keys = new List<byte[]>(_keys);
                        keys.RemoveAt(i);
                        _keys = keys;
                        return;
                    }
                }

                throw new Exception("Key not found in keyring");
            }
        }

        /// <summary>
        /// Gets all keys in the keyring.
        /// </summary>
        /// <returns>List of keys</returns>
        public List<byte[]> GetKeys()
        {
            lock (_lock)
            {
                return new List<byte[]>(_keys);
            }
        }

        /// <summary>
        /// Gets the primary key used for encryption.
        /// </summary>
        /// <returns>Primary key or null if keyring is empty</returns>
        public byte[]? GetPrimaryKey()
        {
            lock (_lock)
            {
                return _keys.Count > 0 ? _keys[0] : null;
            }
        }

        /// <summary>
        /// Encrypts a message using AES encryption.
        /// </summary>
        /// <param name="plaintext">Message to encrypt</param>
        /// <param name="extra">Additional data for authentication</param>
        /// <returns>Encrypted message</returns>
        public byte[] Encrypt(byte[] plaintext, byte[] extra)
        {
            return Encrypt(plaintext, extra, GetPrimaryKey());
        }

        /// <summary>
        /// Encrypts a message using AES encryption with a specific key.
        /// </summary>
        /// <param name="plaintext">Message to encrypt</param>
        /// <param name="extra">Additional data for authentication</param>
        /// <param name="key">Key to use for encryption</param>
        /// <returns>Encrypted message</returns>
        public byte[] Encrypt(byte[] plaintext, byte[] extra, byte[]? key)
        {
            if (key == null)
                throw new Exception("No encryption key available");

            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            var ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);

            // Combine IV and ciphertext
            var result = new byte[aes.IV.Length + ciphertext.Length];
            aes.IV.CopyTo(result, 0);
            ciphertext.CopyTo(result, aes.IV.Length);

            return result;
        }

        /// <summary>
        /// Decrypts a message using AES decryption.
        /// </summary>
        /// <param name="ciphertext">Encrypted message</param>
        /// <param name="extra">Additional data for authentication</param>
        /// <returns>Decrypted message</returns>
        /// <exception cref="Exception">If decryption fails</exception>
        public byte[] Decrypt(byte[] ciphertext, byte[] extra)
        {
            var keys = GetKeys();
            if (keys.Count == 0)
                throw new Exception("No decryption keys available");

            using var aes = Aes.Create();
            var ivLength = aes.BlockSize / 8;

            if (ciphertext.Length < ivLength)
                throw new Exception("Ciphertext too short");

            var iv = new byte[ivLength];
            Array.Copy(ciphertext, 0, iv, 0, ivLength);

            var encryptedData = new byte[ciphertext.Length - ivLength];
            Array.Copy(ciphertext, ivLength, encryptedData, 0, encryptedData.Length);

            foreach (var key in keys)
            {
                try
                {
                    aes.Key = key;
                    aes.IV = iv;

                    using var decryptor = aes.CreateDecryptor();
                    return decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);
                }
                catch
                {
                    // Try next key
                }
            }

            throw new Exception("Decryption failed with all available keys");
        }

        private void InstallKeys(List<byte[]> keys, byte[] primaryKey)
        {
            var newKeys = new List<byte[]> { primaryKey };
            newKeys.AddRange(keys.Where(k => !k.SequenceEqual(primaryKey)));
            _keys = newKeys;
        }
    }
}
