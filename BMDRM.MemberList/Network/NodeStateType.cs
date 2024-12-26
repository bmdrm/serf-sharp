// Copyright (c) HashiCorp, Inc.
// SPDX-License-Identifier: MPL-2.0

namespace BMDRM.MemberList.Network;

/// <summary>
/// NodeStateType represents the state of a node in the cluster
/// </summary>
public enum NodeStateType
{
    /// <summary>
    /// Node is alive and well
    /// </summary>
    Alive,

    /// <summary>
    /// Node is suspected to be dead
    /// </summary>
    Suspect,

    /// <summary>
    /// Node is confirmed dead
    /// </summary>
    Dead,

    /// <summary>
    /// Node has left the cluster
    /// </summary>
    Left
}
