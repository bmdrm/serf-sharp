using System.Net;
using MessagePack;

namespace BMDRM.MemberList.Network.Messages;

/// <summary>
/// MessageHandoff is used to forward a user message between peers
/// </summary>
[MessagePackObject]
public class MessageHandoff
{
    /// <summary>
    /// Message type
    /// </summary>
    [Key(0)]
    public MessageType MsgType { get; set; }

    /// <summary>
    /// Message buffer
    /// </summary>
    [Key(1)]
    public byte[] Buf { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Source address
    /// </summary>
    [Key(2)]
    public EndPoint From { get; set; } = default!;
}
