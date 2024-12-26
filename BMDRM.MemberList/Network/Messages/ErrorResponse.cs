// Copyright (c) HashiCorp, Inc.
// SPDX-License-Identifier: MPL-2.0

namespace BMDRM.MemberList.Network.Messages;

/// <summary>
/// Error response is sent to relay the error from the remote end
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Error message
    /// </summary>
    public string Error { get; set; } = string.Empty;
}
