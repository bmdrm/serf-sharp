// Copyright (c) HashiCorp, Inc.
// SPDX-License-Identifier: MPL-2.0

namespace BMDRM.MemberList.Network.Messages;

/// <summary>
/// Ack response is sent for a ping
/// </summary>
public class AckResponse
{
    /// <summary>
    /// Sequence number for the ping
    /// </summary>
    public uint SeqNo { get; set; }

    /// <summary>
    /// Optional payload
    /// </summary>
    public byte[]? Payload { get; set; }
}
