using MessagePack;
using BMDRM.MemberList.Network;

namespace BMDRM.MemberList.Network.Messages;

/// <summary>
/// Compress is used to wrap an underlying payload using a specified compression algorithm
/// </summary>
[MessagePackObject]
public class Compress
{
    /// <summary>
    /// Compression algorithm to use
    /// </summary>
    [Key(0)]
    public Network.CompressionType Algo { get; set; }

    /// <summary>
    /// Compressed data buffer
    /// </summary>
    [Key(1)]
    public byte[] Buf { get; set; } = Array.Empty<byte>();
}
