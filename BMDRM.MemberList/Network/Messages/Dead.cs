using MessagePack;

namespace BMDRM.MemberList.Network.Messages;

/// <summary>
/// Dead is broadcast when we confirm a node is dead
/// Overloaded for nodes leaving
/// </summary>
[MessagePackObject]
public class Dead
{
    /// <summary>
    /// Incarnation number
    /// </summary>
    [Key(0)]
    public uint Incarnation { get; set; }

    /// <summary>
    /// Node name
    /// </summary>
    [Key(1)]
    public string Node { get; set; } = string.Empty;

    /// <summary>
    /// Include who is suspecting
    /// </summary>
    [Key(2)]
    public string From { get; set; } = string.Empty;
}
