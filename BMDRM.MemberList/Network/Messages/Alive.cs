using MessagePack;
using BMDRM.MemberList.State;

namespace BMDRM.MemberList.Network.Messages;

/// <summary>
/// Alive is used for nodes that are reporting that they are alive
/// </summary>
[MessagePackObject]
public class Alive
{
    /// <summary>
    /// Node name
    /// </summary>
    [Key(0)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Node address
    /// </summary>
    [Key(1)]
    public string Addr { get; set; } = string.Empty;

    /// <summary>
    /// Node port
    /// </summary>
    [Key(2)]
    public int Port { get; set; }

    /// <summary>
    /// Incarnation number
    /// </summary>
    [Key(3)]
    public ulong Incarnation { get; set; }

    /// <summary>
    /// Node state
    /// </summary>
    [Key(4)]
    public NodeStateType State { get; set; }
}
