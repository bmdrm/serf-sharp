// Copyright (c) HashiCorp, Inc.
// SPDX-License-Identifier: MPL-2.0

namespace BMDRM.MemberList.Network.Messages;

/// <summary>
/// Suspect is broadcast when we suspect a node is dead
/// </summary>
public class Suspect
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
