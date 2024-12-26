using MessagePack;

namespace BMDRM.MemberList.Network.Messages;

/// <summary>
/// ErrorResponse is used to return an error
/// </summary>
[MessagePackObject]
public class ErrorResponse
{
    /// <summary>
    /// Error message
    /// </summary>
    [Key(0)]
    public string Error { get; set; } = string.Empty;
}
