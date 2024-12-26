// Copyright (c) HashiCorp, Inc.
// SPDX-License-Identifier: MPL-2.0

using MessagePack;

namespace BMDRM.MemberList.Network.Messages;

/// <summary>
/// Suspect is broadcast when we suspect a node is dead
/// </summary>
[MessagePackObject]
public class Suspect
{
    /// <summary>
    /// Node name
    /// </summary>
    [Key(0)]
    public string Node { get; set; } = string.Empty;

    /// <summary>
    /// Include who is suspecting
    /// </summary>
    [Key(1)]
    public string From { get; set; } = string.Empty;

    /// <summary>
    /// Incarnation number
    /// </summary>
    [Key(2)]
    public ulong Incarnation { get; set; }
}
