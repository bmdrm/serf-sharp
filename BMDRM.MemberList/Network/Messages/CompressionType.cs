// Copyright (c) HashiCorp, Inc.
// SPDX-License-Identifier: MPL-2.0

namespace BMDRM.MemberList.Network.Messages;

/// <summary>
/// CompressionType is used to specify the compression algorithm
/// </summary>
public enum CompressionType
{
    /// <summary>
    /// No compression
    /// </summary>
    None = 0,

    /// <summary>
    /// LZ4 compression
    /// </summary>
    Lz4 = 1
}
