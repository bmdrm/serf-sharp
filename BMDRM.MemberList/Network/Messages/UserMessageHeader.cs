// Copyright (c) HashiCorp, Inc.
// SPDX-License-Identifier: MPL-2.0

namespace BMDRM.MemberList.Network.Messages;

/// <summary>
/// UserMessageHeader is used to encapsulate a userMsg
/// </summary>
public class UserMessageHeader
{
    /// <summary>
    /// Encodes the byte length of user state
    /// </summary>
    public int UserMsgLen { get; set; }
}
