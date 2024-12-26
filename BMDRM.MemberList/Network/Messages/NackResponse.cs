// Copyright (c) HashiCorp, Inc.
// SPDX-License-Identifier: MPL-2.0

namespace BMDRM.MemberList.Network.Messages;

/// <summary>
/// Nack response is sent for an indirect ping when the pinger doesn't hear from
/// the ping-ee within the configured timeout. This lets the original node know
/// that the indirect ping attempt happened but didn't succeed.
/// </summary>
public class NackResponse
{
    /// <summary>
    /// Sequence number for the ping
    /// </summary>
    public uint SeqNo { get; set; }
}
