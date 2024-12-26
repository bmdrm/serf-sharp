using MessagePack;

namespace BMDRM.MemberList.Network.Messages;

/// <summary>
/// UserMessageHeader is used to encapsulate a user message
/// </summary>
[MessagePackObject]
public class UserMessageHeader
{
    /// <summary>
    /// User message data
    /// </summary>
    [Key(0)]
    public byte[] Data { get; set; } = [];
}
