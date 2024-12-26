using MessagePack;

namespace BMDRM.MemberList.Network.Messages;

/// <summary>
/// Ping request sent directly to node
/// </summary>
[MessagePackObject]
public class Ping
{
    /// <summary>
    /// Sequence number for the ping
    /// </summary>
    [Key(0)]
    public uint SeqNo { get; set; }

    /// <summary>
    /// Node is sent so the target can verify they are
    /// the intended recipient. This is to protect against an agent
    /// restart with a new name.
    /// </summary>
    [Key(1)]
    public string Node { get; set; } = string.Empty;

    /// <summary>
    /// Source address, used for a direct reply
    /// </summary>
    [Key(2)]
    public byte[]? SourceAddr { get; set; }

    /// <summary>
    /// Source port, used for a direct reply
    /// </summary>
    [Key(3)]
    public ushort SourcePort { get; set; }

    /// <summary>
    /// Source name, used for a direct reply
    /// </summary>
    [Key(4)]
    public string? SourceNode { get; set; }
}
