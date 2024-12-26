// Copyright (c) HashiCorp, Inc.
// SPDX-License-Identifier: MPL-2.0

using MessagePack;

namespace BMDRM.MemberList.Network.Messages;

/// <summary>
/// PushPullHeader is used to inform the state sync
/// </summary>
[MessagePackObject]
public class PushPullHeader
{
    /// <summary>
    /// Number of states
    /// </summary>
    [Key(0)]
    public int StateCount { get; set; }

    /// <summary>
    /// User state data
    /// </summary>
    [Key(1)]
    public byte[] UserState { get; set; } = Array.Empty<byte>();
}
