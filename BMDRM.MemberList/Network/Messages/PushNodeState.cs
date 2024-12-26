// Copyright (c) HashiCorp, Inc.
// SPDX-License-Identifier: MPL-2.0

namespace BMDRM.MemberList.Network.Messages;

/// <summary>
/// PushNodeState is used for pushPullReq when we are transferring out node states
/// </summary>
public class PushNodeState
{
    /// <summary>
    /// Node name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Node address
    /// </summary>
    public byte[] Addr { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Node port
    /// </summary>
    public ushort Port { get; set; }

    /// <summary>
    /// Node metadata
    /// </summary>
    public byte[]? Meta { get; set; }

    /// <summary>
    /// Incarnation number
    /// </summary>
    public uint Incarnation { get; set; }

    /// <summary>
    /// Node state type
    /// </summary>
    public NodeStateType State { get; set; }

    /// <summary>
    /// Protocol versions
    /// </summary>
    public byte[] Vsn { get; set; } = Array.Empty<byte>();
}
