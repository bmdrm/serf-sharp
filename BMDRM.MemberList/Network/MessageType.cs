// Copyright (c) HashiCorp, Inc.
// SPDX-License-Identifier: MPL-2.0

namespace BMDRM.MemberList.Network;

/// <summary>
/// MessageType is an integer ID of a type of message that can be received
/// on network channels from other members.
/// </summary>
public enum MessageType : byte
{
    // WARNING: ONLY APPEND TO THIS LIST! The numeric values are part of the
    // protocol itself.
    Ping = 0,
    IndirectPing,
    AckResp,
    Suspect,
    Alive,
    Dead,
    PushPull,
    Compound,
    User, // User message, not handled by us
    Compress,
    Encrypt,
    NackResp,
    HasCrc,
    Err,

    // HasLabel has a deliberately high value so that you can disambiguate
    // it from the encryptionVersion header which is either 0/1 right now and
    // also any of the existing messageTypes
    HasLabel = 244
}
