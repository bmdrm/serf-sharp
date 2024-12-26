// Copyright (c) HashiCorp, Inc.
// SPDX-License-Identifier: MPL-2.0

namespace BMDRM.MemberList.Network.Messages;

/// <summary>
/// Dead is broadcast when we confirm a node is dead
/// Overloaded for nodes leaving
/// </summary>
public class Dead
{
    /// <summary>
    /// Incarnation number
    /// </summary>
    public uint Incarnation { get; set; }

    /// <summary>
    /// Node name
    /// </summary>
    public string Node { get; set; } = string.Empty;

    /// <summary>
    /// Include who is suspecting
    /// </summary>
    public string From { get; set; } = string.Empty;
}
