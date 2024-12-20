using System;
using System.Linq;
using BMDRM.MemberList.Utils;
using Xunit;

namespace BMDRM.MemberList.Tests.Utils
{
    public class KeyringTests
    {
        private static readonly byte[][] TestKeys = new[]
        {
            new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 },
            new byte[] { 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0 },
            new byte[] { 8, 9, 10, 11, 12, 13, 14, 15, 0, 1, 2, 3, 4, 5, 6, 7 }
        };

        [Fact]
        public void EmptyRing_ShouldCreateKeyringWithNoKeys()
        {
            // Arrange & Act
            var keyring = new Keyring(Array.Empty<byte[]>(), null);

            // Assert
            var keys = keyring.GetKeys();
            Assert.Empty(keys);
        }

        [Fact]
        public void PrimaryOnly_ShouldCreateKeyringWithOnlyPrimaryKey()
        {
            // Arrange & Act
            var keyring = new Keyring(Array.Empty<byte[]>(), TestKeys[0]);

            // Assert
            var keys = keyring.GetKeys();
            Assert.Single(keys);
        }

        [Fact]
        public void GetPrimaryKey_ShouldReturnCorrectKey()
        {
            // Arrange
            var keyring = new Keyring(TestKeys, TestKeys[1]);

            // Act
            var primaryKey = keyring.GetPrimaryKey();

            // Assert
            Assert.Equal(TestKeys[1], primaryKey);
        }

        [Fact]
        public void UseKey_NonExistentKey_ShouldThrowException()
        {
            // Arrange
            var keyring = new Keyring(Array.Empty<byte[]>(), TestKeys[1]);

            // Act & Assert
            Assert.Throws<Exception>(() => keyring.UseKey(TestKeys[2]));
        }

        [Fact]
        public void AddKey_ShouldAddKeyToRing()
        {
            // Arrange
            var keyring = new Keyring(Array.Empty<byte[]>(), TestKeys[1]);

            // Act
            keyring.AddKey(TestKeys[2]);

            // Assert
            var keys = keyring.GetKeys();
            Assert.Equal(2, keys.Count);
            Assert.Equal(TestKeys[1], keys[0]); // Primary key should remain unchanged
            Assert.Contains(TestKeys[2], keys);
        }

        [Fact]
        public void UseKey_ExistingKey_ShouldMakeKeyPrimary()
        {
            // Arrange
            var keyring = new Keyring(Array.Empty<byte[]>(), TestKeys[1]);
            keyring.AddKey(TestKeys[2]);

            // Act
            keyring.UseKey(TestKeys[2]);

            // Assert
            Assert.Equal(TestKeys[2], keyring.GetPrimaryKey());
        }

        [Fact]
        public void RemoveKey_PrimaryKey_ShouldThrowException()
        {
            // Arrange
            var keyring = new Keyring(Array.Empty<byte[]>(), TestKeys[1]);
            keyring.AddKey(TestKeys[2]);
            keyring.UseKey(TestKeys[2]);

            // Act & Assert
            Assert.Throws<Exception>(() => keyring.RemoveKey(TestKeys[2]));
        }

        [Fact]
        public void RemoveKey_NonPrimaryKey_ShouldSucceed()
        {
            // Arrange
            var keyring = new Keyring(Array.Empty<byte[]>(), TestKeys[1]);
            keyring.AddKey(TestKeys[2]);
            keyring.UseKey(TestKeys[2]); // Make TestKeys[2] the primary key

            // Act
            keyring.RemoveKey(TestKeys[1]); // Now TestKeys[1] is not the primary key

            // Assert
            var keys = keyring.GetKeys();
            Assert.Single(keys);
            Assert.Equal(TestKeys[2], keys[0]);
        }

        [Fact]
        public void MultiKeyEncryptDecrypt_ShouldWorkWithPrimaryKey()
        {
            // Arrange
            var plaintext = new byte[] { 1, 2, 3, 4, 5 };
            var extra = new byte[] { 6, 7, 8, 9, 10 };
            var keyring = new Keyring(TestKeys, TestKeys[0]);

            // Act
            var encrypted = keyring.Encrypt(plaintext, extra);
            var decrypted = keyring.Decrypt(encrypted, extra);

            // Assert
            Assert.Equal(plaintext, decrypted);
        }

        [Fact]
        public void MultiKeyEncryptDecrypt_ShouldWorkWithSecondaryKey()
        {
            // Arrange
            var plaintext = new byte[] { 1, 2, 3, 4, 5 };
            var extra = new byte[] { 6, 7, 8, 9, 10 };
            var keyring = new Keyring(TestKeys, TestKeys[0]);

            // Act
            // Encrypt with secondary key
            var encrypted = keyring.Encrypt(plaintext, extra, TestKeys[2]);
            var decrypted = keyring.Decrypt(encrypted, extra);

            // Assert
            Assert.Equal(plaintext, decrypted);
        }

        [Fact]
        public void MultiKeyEncryptDecrypt_ShouldFailAfterKeyRemoval()
        {
            // Arrange
            var plaintext = new byte[] { 1, 2, 3, 4, 5 };
            var extra = new byte[] { 6, 7, 8, 9, 10 };
            var keyring = new Keyring(TestKeys, TestKeys[0]);

            // Encrypt with secondary key
            var encrypted = keyring.Encrypt(plaintext, extra, TestKeys[2]);

            // Remove the key used for encryption
            keyring.RemoveKey(TestKeys[2]);

            // Act & Assert
            Assert.Throws<Exception>(() => keyring.Decrypt(encrypted, extra));
        }

        [Fact]
        public void Keyring_ShouldHandleNullKeys()
        {
            // Arrange & Act
            var keyring = new Keyring(null, null);

            // Assert
            var keys = keyring.GetKeys();
            Assert.Empty(keys);
        }

        [Fact]
        public void AddKey_DuplicateKey_ShouldNotAddTwice()
        {
            // Arrange
            var keyring = new Keyring(Array.Empty<byte[]>(), TestKeys[0]);

            // Act
            keyring.AddKey(TestKeys[0]);

            // Assert
            var keys = keyring.GetKeys();
            Assert.Single(keys);
        }

        [Fact]
        public void RemoveKey_NonExistentKey_ShouldThrowException()
        {
            // Arrange
            var keyring = new Keyring(Array.Empty<byte[]>(), TestKeys[0]);

            // Act & Assert
            Assert.Throws<Exception>(() => keyring.RemoveKey(TestKeys[1]));
        }
    }
}
