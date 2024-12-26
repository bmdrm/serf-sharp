using MessagePack;

namespace BMDRM.MemberList.Network.Messages;

/// <summary>
/// NackResponse is used for a nack response
/// </summary>
[MessagePackObject]
public class NackResponse
{
    /// <summary>
    /// The sequence number being responded to
    /// </summary>
    [Key(0)]
    public int SeqNo { get; set; }

    /// <summary>
    /// The source node
    /// </summary>
    [Key(1)]
    public string From { get; set; } = string.Empty;
}
