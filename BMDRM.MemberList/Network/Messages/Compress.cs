// Copyright (c) HashiCorp, Inc.
// SPDX-License-Identifier: MPL-2.0

namespace BMDRM.MemberList.Network.Messages;

/// <summary>
/// Compress is used to wrap an underlying payload using a specified compression algorithm
/// </summary>
public class Compress
{
    /// <summary>
    /// Compression algorithm to use
    /// </summary>
    public CompressionType Algo { get; set; }

    /// <summary>
    /// Compressed data buffer
    /// </summary>
    public byte[] Buf { get; set; } = Array.Empty<byte>();
}
