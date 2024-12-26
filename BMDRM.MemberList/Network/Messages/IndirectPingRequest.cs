using MessagePack;

namespace BMDRM.MemberList.Network.Messages;

/// <summary>
/// Indirect ping sent to an indirect node
/// </summary>
[MessagePackObject]
public class IndirectPingRequest
{
    /// <summary>
    /// Sequence number for the ping
    /// </summary>
    [Key(0)]
    public uint SeqNo { get; set; }

    /// <summary>
    /// Target address
    /// </summary>
    [Key(1)]
    public byte[] Target { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Target port
    /// </summary>
    [Key(2)]
    public ushort Port { get; set; }

    /// <summary>
    /// Node is sent so the target can verify they are
    /// the intended recipient. This is to protect against an agent
    /// restart with a new name.
    /// </summary>
    [Key(3)]
    public string Node { get; set; } = string.Empty;

    /// <summary>
    /// True if we'd like a nack back
    /// </summary>
    [Key(4)]
    public bool Nack { get; set; }

    /// <summary>
    /// Source address, used for a direct reply
    /// </summary>
    [Key(5)]
    public byte[]? SourceAddr { get; set; }

    /// <summary>
    /// Source port, used for a direct reply
    /// </summary>
    [Key(6)]
    public ushort SourcePort { get; set; }

    /// <summary>
    /// Source name, used for a direct reply
    /// </summary>
    [Key(7)]
    public string? SourceNode { get; set; }
}
