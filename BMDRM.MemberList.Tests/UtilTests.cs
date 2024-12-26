// Copyright (c) HashiCorp, Inc.
// SPDX-License-Identifier: MPL-2.0

using System.Net;
using System.Text;
using BMDRM.MemberList.Network;
using BMDRM.MemberList.Network.Messages;
using BMDRM.MemberList.State;
using Xunit;

namespace BMDRM.MemberList.Tests;

public class UtilTests
{
    [Theory]
    [InlineData("1.2.3.4", false, "1.2.3.4:8301")]
    [InlineData("1.2.3.4:1234", true, "1.2.3.4:1234")]
    [InlineData("2600:1f14:e22:1501:f9a:2e0c:a167:67e8", false, "[2600:1f14:e22:1501:f9a:2e0c:a167:67e8]:8301")]
    [InlineData("[2600:1f14:e22:1501:f9a:2e0c:a167:67e8]", false, "[2600:1f14:e22:1501:f9a:2e0c:a167:67e8]:8301")]
    [InlineData("[2600:1f14:e22:1501:f9a:2e0c:a167:67e8]:1234", true, "[2600:1f14:e22:1501:f9a:2e0c:a167:67e8]:1234")]
    [InlineData("localhost", false, "localhost:8301")]
    [InlineData("localhost:1234", true, "localhost:1234")]
    [InlineData("hashicorp.com", false, "hashicorp.com:8301")]
    [InlineData("hashicorp.com:1234", true, "hashicorp.com:1234")]
    public void PortFunctions_Test(string addr, bool hasPort, string ensurePort)
    {
        Assert.Equal(hasPort, Util.HasPort(addr));
        Assert.Equal(ensurePort, Util.EnsurePort(addr, 8301));
    }

    [Fact]
    public void EncodeDecode_Test()
    {
        var msg = new Ping { SeqNo = 100 };
        var buf = Util.Encode(MessageType.Ping, msg);
        var output = Util.Decode<Ping>(buf);

        Assert.NotNull(output);
        Assert.Equal(msg.SeqNo, output.SeqNo);
    }

    [Fact]
    public void RandomOffset_Test()
    {
        var vals = new HashSet<int>();
        for (var i = 0; i < 100; i++)
        {
            var offset = Util.RandomOffset(1 << 30);
            Assert.DoesNotContain(offset, vals);
            vals.Add(offset);
        }
    }

    [Fact]
    public void RandomOffset_Zero_Test()
    {
        var offset = Util.RandomOffset(0);
        Assert.Equal(0, offset);
    }

    [Theory]
    [InlineData(50, 1698)]
    [InlineData(500, 2698)]
    public void SuspicionTimeout_Test(int n, int expectedMs)
    {
        var limit = Util.SuspicionTimeout(3, n, TimeSpan.FromSeconds(1)) / 3;
        var expected = TimeSpan.FromMilliseconds(expectedMs);
        var diff = limit - expected;
        Assert.True(Math.Abs(diff.TotalMilliseconds) < 2, $"Expected {expected}, got {limit}, difference {diff.TotalMilliseconds}ms");
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 3)]
    [InlineData(99, 6)]
    public void RetransmitLimit_Test(int n, int expected)
    {
        var limit = Util.RetransmitLimit(3, n);
        Assert.Equal(expected, limit);
    }

    [Fact]
    public void ShuffleNodes_Test()
    {
        var orig = new List<NodeState>
        {
            new() { State = State.NodeStateType.Dead },
            new() { State = State.NodeStateType.Alive },
            new() { State = State.NodeStateType.Alive },
            new() { State = State.NodeStateType.Dead },
            new() { State = State.NodeStateType.Alive },
            new() { State = State.NodeStateType.Alive },
            new() { State = State.NodeStateType.Dead },
            new() { State = State.NodeStateType.Alive }
        };

        var nodes = new List<NodeState>(orig);
        Assert.Equal(orig, nodes);

        Util.ShuffleNodes(nodes);
        Assert.NotEqual(orig, nodes);
    }

    [Theory]
    [InlineData(32, 1)]
    [InlineData(33, 2)]
    [InlineData(64, 2)]
    [InlineData(65, 3)]
    [InlineData(128, 3)]
    public void PushPullScale_Test(int n, int expectedMultiplier)
    {
        var baseInterval = TimeSpan.FromSeconds(1);
        var scaled = Util.PushPullScale(baseInterval, n);
        Assert.Equal(TimeSpan.FromSeconds(expectedMultiplier), scaled);
    }

    [Fact]
    public void MoveDeadNodes_Test()
    {
        var now = DateTime.UtcNow;
        var nodes = new List<NodeState>
        {
            new() { State = State.NodeStateType.Dead, StateChange = now.AddSeconds(-20) },
            new() { State = State.NodeStateType.Alive, StateChange = now.AddSeconds(-20) },
            // This dead node should not be moved, as its state changed recently
            new() { State = State.NodeStateType.Dead, StateChange = now.AddSeconds(-10) },
            // This left node should not be moved, as its state changed recently
            new() { State = State.NodeStateType.Left, StateChange = now.AddSeconds(-10) },
            new() { State = State.NodeStateType.Left, StateChange = now.AddSeconds(-20) },
            new() { State = State.NodeStateType.Alive, StateChange = now.AddSeconds(-20) },
            new() { State = State.NodeStateType.Dead, StateChange = now.AddSeconds(-20) }
        };

        var remain = Util.MoveDeadNodes(nodes, TimeSpan.FromSeconds(15));

        // Should be 4 remaining nodes (2 alive, 2 recently changed)
        Assert.Equal(4, remain);

        // First 4 should be all alive or recently changed
        for (var i = 0; i < remain; i++)
        {
            Assert.True(nodes[i].State == State.NodeStateType.Alive ||
                       now - nodes[i].StateChange <= TimeSpan.FromSeconds(15));
        }

        // Rest should be old dead/left nodes
        for (var i = remain; i < nodes.Count; i++)
        {
            Assert.True((nodes[i].State == State.NodeStateType.Dead || nodes[i].State == State.NodeStateType.Left) &&
                       now - nodes[i].StateChange > TimeSpan.FromSeconds(15));
        }
    }

    [Fact]
    public void KRandomNodes_Test()
    {
        var nodes = new List<NodeState>();
        for (var i = 0; i < 30; i++)
        {
            nodes.Add(new NodeState { Name = $"node{i}" });
        }

        // Try to select more nodes than we have
        var kNodes = Util.KRandomNodes(40, nodes);
        Assert.True(kNodes.Count <= nodes.Count);

        // Try with a filter
        var filterKNodes = Util.KRandomNodes(3, nodes, n => n.Name.EndsWith("0"));
        Assert.Equal(3, filterKNodes.Count);
        Assert.DoesNotContain(filterKNodes, n => n.Name.EndsWith("0"));

        // Verify no duplicates
        var names = new HashSet<string>();
        foreach (var n in filterKNodes)
        {
            Assert.True(names.Add(n.Name));
        }
    }

    [Fact]
    public void MakeCompoundMessage_Test()
    {
        var msgs = new List<byte[]>
        {
            new byte[] { 1, 2, 3 },
            new byte[] { 4, 5, 6 }
        };

        var msg = Util.MakeCompoundMessage(msgs);
        Assert.Equal((byte)MessageType.Compound, msg[0]);
        Assert.Equal((byte)2, msg[1]); // 2 messages

        var (truncated, parts) = Util.DecodeCompoundMessage(msg);
        Assert.Equal(0, truncated);
        Assert.Equal(2, parts.Count);
        Assert.Equal(msgs[0], parts[0]);
        Assert.Equal(msgs[1], parts[1]);
    }

    [Fact]
    public void DecodeCompoundMessage_Truncated_Test()
    {
        var msgs = new List<byte[]>
        {
            new byte[] { 1, 2, 3 },
            new byte[] { 4, 5, 6 }
        };

        var msg = Util.MakeCompoundMessage(msgs);
        // Truncate the last message
        var truncatedMsg = msg[..(msg.Length - 2)];

        var (truncated, parts) = Util.DecodeCompoundMessage(truncatedMsg);
        Assert.Equal(1, truncated);
        Assert.Single(parts);
        Assert.Equal(msgs[0], parts[0]);
    }

    [Fact]
    public void CompressDecompress_Test()
    {
        var payload = new byte[1024];
        for (var i = 0; i < payload.Length; i++)
        {
            payload[i] = (byte)(i % 256);
        }

        var compressed = Util.CompressPayload(payload);
        var decompressed = Util.DecompressPayload(compressed);

        Assert.Equal(payload, decompressed);
    }
}
