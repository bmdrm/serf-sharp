// Copyright (c) HashiCorp, Inc.
// SPDX-License-Identifier: MPL-2.0

namespace BMDRM.MemberList.Delegates
{
    /// <summary>
    /// NodeEventType are the types of events that can be sent from the
    /// ChannelEventDelegate.
    /// </summary>
    public enum NodeEventType
    {
        NodeJoin,
        NodeLeave,
        NodeUpdate
    }
}
