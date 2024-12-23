using BMDRM.MemberList.Security;
using Xunit;

namespace BMDRM.MemberList.Tests.Security;

public class KeyringTests
{
    private static readonly byte[] Key1 = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
    private static readonly byte[] Key2 = new byte[] { 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0 };
    private static readonly byte[] InvalidKey = new byte[] { 1, 2, 3 }; // Too short

    [Fact]
    public void TestCreate_NoKeys()
    {
        var keyring = Keyring.Create();
        Assert.Empty(keyring.GetKeys());
        Assert.Null(keyring.GetPrimaryKey());
    }

    [Fact]
    public void TestCreate_PrimaryOnly()
    {
        var keyring = Keyring.Create(primaryKey: Key1);
        Assert.Single(keyring.GetKeys());
        Assert.Equal(Key1, keyring.GetPrimaryKey());
    }

    [Fact]
    public void TestCreate_WithSecondaryKeys()
    {
        var keyring = Keyring.Create(new[] { Key2 }, Key1);
        var keys = keyring.GetKeys();
        Assert.Equal(2, keys.Length);
        Assert.Equal(Key1, keys[0]); // Primary key should be first
        Assert.Equal(Key2, keys[1]);
    }

    [Fact]
    public void TestCreate_NoPrimaryKey()
    {
        Assert.Throws<ArgumentException>(() => Keyring.Create(new[] { Key1 }));
    }

    [Fact]
    public void TestValidateKey()
    {
        // Valid key lengths
        Assert.True(Keyring.ValidateKey(new byte[16])); // AES-128
        Assert.True(Keyring.ValidateKey(new byte[24])); // AES-192
        Assert.True(Keyring.ValidateKey(new byte[32])); // AES-256

        // Invalid key lengths
        Assert.False(Keyring.ValidateKey(new byte[0]));
        Assert.False(Keyring.ValidateKey(new byte[15]));
        Assert.False(Keyring.ValidateKey(new byte[17]));
        Assert.False(Keyring.ValidateKey(new byte[33]));
    }

    [Fact]
    public void TestAddKey()
    {
        var keyring = Keyring.Create();

        // Add first key
        Assert.True(keyring.AddKey(Key1));
        Assert.Equal(Key1, keyring.GetPrimaryKey());

        // Add second key
        Assert.True(keyring.AddKey(Key2));
        var keys = keyring.GetKeys();
        Assert.Equal(2, keys.Length);
        Assert.Equal(Key1, keys[0]); // Primary key unchanged
        Assert.Equal(Key2, keys[1]);

        // Try to add invalid key
        Assert.False(keyring.AddKey(InvalidKey));
        Assert.Equal(2, keyring.GetKeys().Length);

        // Try to add duplicate key
        Assert.True(keyring.AddKey(Key1));
        Assert.Equal(2, keyring.GetKeys().Length);
    }

    [Fact]
    public void TestUseKey()
    {
        var keyring = Keyring.Create(new[] { Key2 }, Key1);

        // Use existing key
        Assert.True(keyring.UseKey(Key2));
        Assert.Equal(Key2, keyring.GetPrimaryKey());
        var keys = keyring.GetKeys();
        Assert.Equal(2, keys.Length);
        Assert.Equal(Key2, keys[0]); // New primary key
        Assert.Equal(Key1, keys[1]);

        // Try to use non-existent key
        var key3 = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
        Assert.False(keyring.UseKey(key3));
        Assert.Equal(Key2, keyring.GetPrimaryKey()); // Primary key unchanged
    }

    [Fact]
    public void TestRemoveKey()
    {
        var keyring = Keyring.Create(new[] { Key2 }, Key1);

        // Try to remove primary key
        Assert.False(keyring.RemoveKey(Key1));
        Assert.Equal(2, keyring.GetKeys().Length);

        // Remove secondary key
        Assert.True(keyring.RemoveKey(Key2));
        var keys = keyring.GetKeys();
        Assert.Single(keys);
        Assert.Equal(Key1, keys[0]);

        // Try to remove non-existent key
        Assert.True(keyring.RemoveKey(Key2));
        Assert.Single(keyring.GetKeys());
    }

    [Fact]
    public async Task TestConcurrency()
    {
        var keyring = Keyring.Create(primaryKey: Key1);
        var tasks = new List<Task>();

        // Add keys concurrently
        for (var i = 0; i < 100; i++)
        {
            var key = new byte[16];
            Array.Copy(BitConverter.GetBytes(i), key, 4);
            tasks.Add(Task.Run(() => keyring.AddKey(key)));
        }

        await Task.WhenAll(tasks);

        // All valid keys should be added
        Assert.True(keyring.GetKeys().Length > 1);
        Assert.Equal(Key1, keyring.GetPrimaryKey()); // Primary key should remain unchanged
    }
}
