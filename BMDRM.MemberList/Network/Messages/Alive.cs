// Copyright (c) HashiCorp, Inc.
// SPDX-License-Identifier: MPL-2.0

namespace BMDRM.MemberList.Network.Messages;

/// <summary>
/// Alive is broadcast when we know a node is alive.
/// Overloaded for nodes joining
/// </summary>
public class Alive
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
    /// The versions of the protocol/delegate that are being spoken, order:
    /// pmin, pmax, pcur, dmin, dmax, dcur
    /// </summary>
    public byte[] Vsn { get; set; } = Array.Empty<byte>();
}
