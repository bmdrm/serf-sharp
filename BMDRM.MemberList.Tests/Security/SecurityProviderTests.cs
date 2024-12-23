using System.Text;
using BMDRM.MemberList.Security;
using Xunit;

namespace BMDRM.MemberList.Tests.Security;

public class SecurityProviderTests
{
    [Fact]
    public void TestPKCS7()
    {
        // Test all possible buffer sizes from 0 to 255
        for (var i = 0; i <= 255; i++)
        {
            // Make a buffer of size i
            var buf = new byte[i];
            for (var j = 0; j < i; j++)
            {
                buf[j] = (byte)i;
            }

            // Copy to memory stream
            using var ms = new MemoryStream();
            ms.Write(buf);

            // Call private PKCS7 encode method using reflection
            var type = typeof(SecurityProvider);
            var encodeMethod = type.GetMethod("Pkcs7Encode", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            encodeMethod!.Invoke(null, new object[] { ms, 0, 16 });

            // Call private PKCS7 decode method using reflection
            var decodeMethod = type.GetMethod("Pkcs7Decode",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var dec = (byte[])decodeMethod!.Invoke(null, new object[] { ms.ToArray(), 16 })!;

            // Ensure equivalence
            Assert.Equal(buf, dec);
        }
    }

    [Fact]
    public void TestEncryptDecrypt_V0()
    {
        EncryptDecryptVersioned(EncryptionVersion.MinVersion);
    }

    [Fact]
    public void TestEncryptDecrypt_V1()
    {
        EncryptDecryptVersioned(EncryptionVersion.MaxVersion);
    }

    private void EncryptDecryptVersioned(EncryptionVersion vsn)
    {
        var k1 = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
        var plaintext = Encoding.UTF8.GetBytes("this is a plain text message");
        var extra = Encoding.UTF8.GetBytes("random data");

        using var buf = new MemoryStream();
        SecurityProvider.EncryptPayload(vsn, k1, plaintext, extra, buf);

        var expLen = SecurityProvider.EncryptedLength(vsn, plaintext.Length);
        Assert.Equal(expLen, buf.Length);

        var msg = SecurityProvider.DecryptPayload(new[] { k1 }, buf.ToArray(), extra);
        Assert.Equal(plaintext, msg);
    }

    [Fact]
    public void TestEncryptDecrypt_MultipleKeys()
    {
        var k1 = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
        var k2 = new byte[] { 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0 };
        var plaintext = Encoding.UTF8.GetBytes("this is a plain text message");
        var extra = Encoding.UTF8.GetBytes("random data");

        // Encrypt with k1
        using var buf = new MemoryStream();
        SecurityProvider.EncryptPayload(EncryptionVersion.MaxVersion, k1, plaintext, extra, buf);

        // Should decrypt with either key
        var msg1 = SecurityProvider.DecryptPayload(new[] { k1, k2 }, buf.ToArray(), extra);
        Assert.Equal(plaintext, msg1);

        var msg2 = SecurityProvider.DecryptPayload(new[] { k2, k1 }, buf.ToArray(), extra);
        Assert.Equal(plaintext, msg2);

        // Should fail with wrong key
        Assert.Throws<System.Security.Cryptography.CryptographicException>(() =>
            SecurityProvider.DecryptPayload(new[] { k2 }, buf.ToArray(), extra));
    }

    [Fact]
    public void TestEncryptDecrypt_InvalidInput()
    {
        var k1 = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
        var plaintext = Encoding.UTF8.GetBytes("this is a plain text message");
        var extra = Encoding.UTF8.GetBytes("random data");

        // Test empty payload
        Assert.Throws<ArgumentException>(() =>
            SecurityProvider.DecryptPayload(new[] { k1 }, Array.Empty<byte>(), extra));

        // Test invalid version
        Assert.Throws<ArgumentException>(() =>
            SecurityProvider.DecryptPayload(new[] { k1 }, new byte[] { 3 }, extra));

        // Test payload too small
        Assert.Throws<ArgumentException>(() =>
            SecurityProvider.DecryptPayload(new[] { k1 }, new byte[] { 0, 1, 2 }, extra));

        // Test invalid overhead calculation
        Assert.Throws<ArgumentException>(() =>
            SecurityProvider.EncryptOverhead((EncryptionVersion)3));
    }

    [Fact]
    public void TestEncryptDecrypt_DifferentExtraData()
    {
        var k1 = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
        var plaintext = Encoding.UTF8.GetBytes("this is a plain text message");
        var extra = Encoding.UTF8.GetBytes("random data");
        var wrongExtra = Encoding.UTF8.GetBytes("wrong data");

        using var buf = new MemoryStream();
        SecurityProvider.EncryptPayload(EncryptionVersion.MaxVersion, k1, plaintext, extra, buf);

        // Should fail with wrong extra data
        Assert.Throws<System.Security.Cryptography.CryptographicException>(() =>
            SecurityProvider.DecryptPayload(new[] { k1 }, buf.ToArray(), wrongExtra));
    }
}
