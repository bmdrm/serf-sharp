namespace BMDRM.MemberList.Network;

/// <summary>
/// CompressionType is used to specify the compression algorithm
/// </summary>
public enum CompressionType : byte
{
    /// <summary>
    /// No compression
    /// </summary>
    None = 0,

    /// <summary>
    /// LZ4 compression
    /// </summary>
    Lz4 = 1,

    /// <summary>
    /// LZW compression
    /// </summary>
    LzwAlgo = 2
}
