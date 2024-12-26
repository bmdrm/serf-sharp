using System.Collections.Concurrent;

namespace BMDRM.MemberList.Security;

/// <summary>
/// Keyring contains all key data used internally by memberlist.
/// The first key (primary key) is used for encrypting messages and is the first key tried during decryption.
/// </summary>
public class Keyring
{
    private readonly ConcurrentQueue<byte[]> _keys = new();
    private byte[]? _primaryKey;

    /// <summary>
    /// Constructs a new container for a set of encryption keys.
    /// While creating a new keyring, you must do one of:
    ///   - Omit keys and primary key, effectively disabling encryption
    ///   - Pass a set of keys plus the primary key
    ///   - Pass only a primary key
    /// </summary>
    /// <param name="keys">Optional list of secondary keys</param>
    /// <param name="primaryKey">Primary key used for encryption</param>
    public static Keyring Create(IEnumerable<byte[]>? keys = null, byte[]? primaryKey = null)
    {
        var keyring = new Keyring();

        if (keys == null && primaryKey == null)
        {
            return keyring;
        }

        if (primaryKey == null)
        {
            throw new ArgumentException("Empty primary key not allowed");
        }

        // Add primary key first
        if (!keyring.AddKey(primaryKey))
        {
            throw new ArgumentException("Failed to add primary key");
        }

        // Add secondary keys
        if (keys != null)
        {
            foreach (var key in keys)
            {
                if (!keyring.AddKey(key))
                {
                    throw new ArgumentException($"Failed to add key of length {key.Length}");
                }
            }
        }

        return keyring;
    }

    /// <summary>
    /// Validates if a key has the correct length for AES.
    /// Key should be either 16, 24, or 32 bytes to select AES-128, AES-192, or AES-256.
    /// </summary>
    public static bool ValidateKey(byte[] key)
    {
        return key.Length is 16 or 24 or 32;
    }

    /// <summary>
    /// Installs a new key on the ring. Adding a key to the ring will make it available for use in decryption.
    /// If the key already exists on the ring, this function will just return false.
    /// </summary>
    public bool AddKey(byte[] key)
    {
        if (!ValidateKey(key))
        {
            return false;
        }

        // Check if key already exists
        if (_keys.Any(k => k.SequenceEqual(key)))
        {
            return true;
        }

        // If this is the first key, make it primary
        _primaryKey ??= key;

        _keys.Enqueue(key);
        return true;
    }

    /// <summary>
    /// Changes the key used to encrypt messages. This is the only key used to encrypt messages,
    /// so peers should know this key before this method is called.
    /// </summary>
    public bool UseKey(byte[] key)
    {
        // Check if key exists in the queue
        if (!_keys.Any(k => k.SequenceEqual(key)))
        {
            return false;
        }

        // Update primary key
        _primaryKey = key;

        // Reorder keys to put primary key first
        var keys = _keys.ToArray();
        _keys.Clear();
        
        // Add primary key first
        _keys.Enqueue(key);
        
        // Add remaining keys
        foreach (var k in keys.Where(k => !k.SequenceEqual(key)))
        {
            _keys.Enqueue(k);
        }

        return true;
    }

    /// <summary>
    /// Drops a key from the keyring. This will return false if the key
    /// requested for removal is currently the primary key.
    /// </summary>
    public bool RemoveKey(byte[] key)
    {
        // Cannot remove primary key
        if (_primaryKey != null && _primaryKey.SequenceEqual(key))
        {
            return false;
        }

        // Remove key if it exists
        var keys = _keys.Where(k => !k.SequenceEqual(key)).ToArray();
        _keys.Clear();
        foreach (var k in keys)
        {
            _keys.Enqueue(k);
        }

        return true;
    }

    /// <summary>
    /// Returns the current set of keys on the ring.
    /// </summary>
    public byte[][] GetKeys()
    {
        return _keys.ToArray();
    }

    /// <summary>
    /// Returns the primary key. This is the key used for encrypting messages,
    /// and is the first key tried for decrypting messages.
    /// </summary>
    public byte[]? GetPrimaryKey()
    {
        return _primaryKey;
    }
}
