// Copyright (c) HashiCorp, Inc.
// SPDX-License-Identifier: MPL-2.0

namespace BMDRM.MemberList.Network.Messages;

/// <summary>
/// PushPullHeader is used to inform the otherside how many states we are transferring
/// </summary>
public class PushPullHeader
{
    /// <summary>
    /// Number of nodes
    /// </summary>
    public int Nodes { get; set; }

    /// <summary>
    /// Encodes the byte length of user state
    /// </summary>
    public int UserStateLen { get; set; }

    /// <summary>
    /// Is this a join request or a anti-entropy run
    /// </summary>
    public bool Join { get; set; }
}
