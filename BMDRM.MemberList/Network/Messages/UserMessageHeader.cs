// Copyright (c) HashiCorp, Inc.
// SPDX-License-Identifier: MPL-2.0

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
    public byte[] Data { get; set; } = Array.Empty<byte>();
}
