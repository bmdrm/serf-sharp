// Copyright (c) HashiCorp, Inc.
// SPDX-License-Identifier: MPL-2.0

namespace BMDRM.MemberList.Network.Messages;

/// <summary>
/// Ping request sent directly to node
/// </summary>
public class Ping
{
    /// <summary>
    /// Sequence number for the ping
    /// </summary>
    public uint SeqNo { get; set; }

    /// <summary>
    /// Node is sent so the target can verify they are
    /// the intended recipient. This is to protect against an agent
    /// restart with a new name.
    /// </summary>
    public string Node { get; set; } = string.Empty;

    /// <summary>
    /// Source address, used for a direct reply
    /// </summary>
    public byte[]? SourceAddr { get; set; }

    /// <summary>
    /// Source port, used for a direct reply
    /// </summary>
    public ushort SourcePort { get; set; }

    /// <summary>
    /// Source name, used for a direct reply
    /// </summary>
    public string? SourceNode { get; set; }
}
