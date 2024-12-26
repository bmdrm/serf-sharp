using MessagePack;

namespace BMDRM.MemberList.Network.Messages;

/// <summary>
/// Ack response is sent for a ping
/// </summary>
[MessagePackObject]
public class AckResponse
{
    /// <summary>
    /// Sequence number for the ping
    /// </summary>
    [Key(0)]
    public uint SeqNo { get; set; }

    /// <summary>
    /// Optional payload
    /// </summary>
    [Key(1)]
    public byte[]? Payload { get; set; }
}
