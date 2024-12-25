// Copyright (c) HashiCorp, Inc.
// SPDX-License-Identifier: MPL-2.0

using System;
using BMDRM.MemberList.State;

namespace BMDRM.MemberList.Delegates
{
    /// <summary>
    /// PingDelegate is used to notify an observer how long it took for a ping message to
    /// complete a round trip. It can also be used for writing arbitrary byte slices
    /// into ack messages. Note that in order to be meaningful for RTT estimates, this
    /// delegate does not apply to indirect pings, nor fallback pings sent over TCP.
    /// </summary>
    public interface IPingDelegate
    {
        /// <summary>
        /// AckPayload is invoked when an ack is being sent; the returned bytes will be appended to the ack
        /// </summary>
        /// <returns>Byte array to be appended to the ack message</returns>
        byte[] AckPayload();

        /// <summary>
        /// NotifyPing is invoked when an ack for a ping is received
        /// </summary>
        /// <param name="other">The node that was pinged</param>
        /// <param name="rtt">The round-trip time of the ping</param>
        /// <param name="payload">The payload that was included in the ack</param>
        void NotifyPingComplete(Node other, TimeSpan rtt, byte[] payload);
    }
}
