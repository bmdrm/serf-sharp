// Copyright (c) HashiCorp, Inc.
// SPDX-License-Identifier: MPL-2.0

using System.Net;

namespace BMDRM.MemberList.Network.Messages;

/// <summary>
/// MessageHandoff is used to transfer a message between goroutines
/// </summary>
public class MessageHandoff
{
    /// <summary>
    /// Message type
    /// </summary>
    public MessageType MsgType { get; set; }

    /// <summary>
    /// Message buffer
    /// </summary>
    public byte[] Buf { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Source address
    /// </summary>
    public EndPoint From { get; set; } = default!;
}
