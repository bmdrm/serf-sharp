// Copyright (c) HashiCorp, Inc.
// SPDX-License-Identifier: MPL-2.0

using System.Buffers.Binary;
using System.IO.Compression;
using System.Net;
using System.Text;
using BMDRM.MemberList.Network;
using BMDRM.MemberList.Network.Messages;
using BMDRM.MemberList.State;
using K4os.Compression.LZ4;
using MessagePack;
using CompressionType = BMDRM.MemberList.Network.CompressionType;

namespace BMDRM.MemberList;

/// <summary>
/// Utility functions for the memberlist implementation
/// </summary>
public static class Util
{
    private static readonly Random Random = new();

    #region Constants

    /// <summary>
    /// Minimum number of nodes before we start scaling the push/pull timing.
    /// The scale effect is log2(Nodes) - log2(PushPullScaleThreshold).
    /// This means that the 33rd node will cause us to double the interval,
    /// while the 65th will triple it.
    /// </summary>
    public const int PushPullScaleThreshold = 32;

    /// <summary>
    /// Constant litWidth for LZW compression (2-8)
    /// </summary>
    public const int LzwLitWidth = 8;

    /// <summary>
    /// Maximum number of messages in a compound message
    /// </summary>
    public const int MaxCompoundMessages = 255;

    #endregion

    #region MessagePack

    /// <summary>
    /// Decode reverses the encode operation on a byte slice input
    /// </summary>
    public static T? Decode<T>(byte[] buf)
    {
        return MessagePackSerializer.Deserialize<T>(buf.AsMemory()[1..]);
    }

    /// <summary>
    /// Encode writes an encoded object to a new bytes buffer
    /// </summary>
    public static byte[] Encode<T>(MessageType msgType, T obj)
    {
        var encoded = MessagePackSerializer.Serialize(obj);
        var result = new byte[encoded.Length + 1];
        result[0] = (byte)msgType;
        Buffer.BlockCopy(encoded, 0, result, 1, encoded.Length);
        return result;
    }

    #endregion

    #region Node Management

    /// <summary>
    /// Returns a random offset between 0 and n
    /// </summary>
    public static int RandomOffset(int n)
    {
        return n == 0 ? 0 : Random.Next(n);
    }

    /// <summary>
    /// Randomly shuffles the input nodes using the Fisher-Yates shuffle
    /// </summary>
    public static void ShuffleNodes(IList<NodeState> nodes)
    {
        var n = nodes.Count;
        for (var i = n - 1; i > 0; i--)
        {
            var j = Random.Next(i + 1);
            (nodes[i], nodes[j]) = (nodes[j], nodes[i]);
        }
    }

    /// <summary>
    /// Moves dead and left nodes that have not changed during the gossipToTheDeadTime interval
    /// to the end of the slice and returns the index of the first moved node.
    /// </summary>
    public static int MoveDeadNodes(IList<NodeState> nodes, TimeSpan gossipToTheDeadTime)
    {
        var numDead = 0;
        var n = nodes.Count;
        for (var i = 0; i < n - numDead; i++)
        {
            if (!nodes[i].DeadOrLeft())
            {
                continue;
            }

            // Respect the gossip to the dead interval
            if (DateTime.UtcNow - nodes[i].StateChange <= gossipToTheDeadTime)
            {
                continue;
            }

            // Move this node to the end
            (nodes[i], nodes[n - numDead - 1]) = (nodes[n - numDead - 1], nodes[i]);
            numDead++;
            i--;
        }
        return n - numDead;
    }

    /// <summary>
    /// Selects up to k random Nodes, excluding any nodes where the exclude function returns true.
    /// It is possible that less than k nodes are returned.
    /// </summary>
    public static List<NodeState> KRandomNodes(int k, IList<NodeState> nodes, Func<NodeState, bool>? exclude = null)
    {
        var n = nodes.Count;
        var kNodes = new List<NodeState>(k);

        // Probe up to 3*n times, with large n this is not necessary
        // since k << n, but with small n we want search to be exhaustive
        for (var i = 0; i < 3 * n && kNodes.Count < k; i++)
        {
            // Get random node state
            var idx = RandomOffset(n);
            var state = nodes[idx];

            // Give the filter a shot at it
            if (exclude != null && exclude(state))
            {
                continue;
            }

            // Check if we have this node already
            if (kNodes.Any(node => state.Name == node.Name))
            {
                continue;
            }

            // Append the node
            kNodes.Add(state);
        }

        return kNodes;
    }

    #endregion

    #region Scaling

    /// <summary>
    /// Computes the timeout that should be used when a node is suspected
    /// </summary>
    public static TimeSpan SuspicionTimeout(int suspicionMult, int n, TimeSpan interval)
    {
        var nodeScale = Math.Max(1.0, Math.Log10(Math.Max(1.0, n)));
        // multiply by 1000 to keep some precision because TimeSpan ticks are long
        var timeoutMs = suspicionMult * nodeScale * interval.TotalMilliseconds;
        return TimeSpan.FromMilliseconds(timeoutMs);
    }

    /// <summary>
    /// Computes the limit of retransmissions
    /// </summary>
    public static int RetransmitLimit(int retransmitMult, int n)
    {
        var nodeScale = Math.Ceiling(Math.Log10(n + 1));
        var limit = retransmitMult * (int)nodeScale;
        return limit;
    }

    /// <summary>
    /// Scales the time interval at which push/pull syncs take place.
    /// It is used to prevent network saturation as the cluster size grows
    /// </summary>
    public static TimeSpan PushPullScale(TimeSpan interval, int n)
    {
        // Don't scale until we cross the threshold
        if (n <= PushPullScaleThreshold)
        {
            return interval;
        }

        var multiplier = Math.Ceiling(Math.Log2(n) - Math.Log2(PushPullScaleThreshold)) + 1.0;
        return TimeSpan.FromTicks((long)(multiplier * interval.Ticks));
    }

    #endregion

    #region Network

    /// <summary>
    /// Joins host and port into a host:port form for use with a transport
    /// </summary>
    public static string JoinHostPort(string host, ushort port)
    {
        return $"{host}:{port}";
    }

    /// <summary>
    /// Returns true if the string includes a port.
    /// Handles formats: "host", "host:port", "ipv6::address", "[ipv6::address]:port"
    /// </summary>
    public static bool HasPort(string s)
    {
        if (s.StartsWith("["))
        {
            return s.EndsWith("]") ? false : s.IndexOf(":", s.LastIndexOf("]")) > -1;
        }
        return s.Count(c => c == ':') == 1;
    }

    /// <summary>
    /// Makes sure the given string has a port number on it, otherwise it
    /// appends the given port as a default
    /// </summary>
    public static string EnsurePort(string s, int port)
    {
        if (HasPort(s))
        {
            return s;
        }

        if (s.Contains(':') && !s.StartsWith('['))
        {
            // IPv6 address without port
            return $"[{s}]:{port}";
        }

        return $"{s}:{port}";
    }

    #endregion

    #region Compound Messages

    /// <summary>
    /// Takes a list of messages and packs them into one or multiple messages based on the
    /// limitations of compound messages (255 messages each).
    /// </summary>
    public static List<byte[]> MakeCompoundMessages(IList<byte[]> msgs)
    {
        var bufs = new List<byte[]>((msgs.Count + (MaxCompoundMessages - 1)) / MaxCompoundMessages);

        for (var i = 0; i < msgs.Count; i += MaxCompoundMessages)
        {
            var count = Math.Min(MaxCompoundMessages, msgs.Count - i);
            bufs.Add(MakeCompoundMessage(msgs.Skip(i).Take(count).ToList()));
        }

        return bufs;
    }

    /// <summary>
    /// Takes a list of messages and generates a single compound message containing all of them
    /// </summary>
    public static byte[] MakeCompoundMessage(IList<byte[]> msgs)
    {
        // Calculate total size needed
        var totalSize = 1 + 1; // type + count
        totalSize += msgs.Count * 2; // length prefixes
        totalSize += msgs.Sum(m => m.Length); // message contents

        var buf = new byte[totalSize];
        var pos = 0;

        // Write type
        buf[pos++] = (byte)MessageType.Compound;

        // Write count
        buf[pos++] = (byte)msgs.Count;

        // Write message lengths
        foreach (var msg in msgs)
        {
            BinaryPrimitives.WriteUInt16BigEndian(buf.AsSpan(pos), (ushort)msg.Length);
            pos += 2;
        }

        // Write messages
        foreach (var msg in msgs)
        {
            Buffer.BlockCopy(msg, 0, buf, pos, msg.Length);
            pos += msg.Length;
        }

        return buf;
    }

    /// <summary>
    /// Splits a compound message and returns the slices of individual messages.
    /// Also returns the number of truncated messages and any potential error.
    /// </summary>
    public static (int truncated, List<byte[]> parts) DecodeCompoundMessage(byte[] buf)
    {
        if (buf.Length < 2)
        {
            throw new ArgumentException("Buffer too short", nameof(buf));
        }

        var truncated = 0;
        var parts = new List<byte[]>();
        var pos = 1; // Skip the message type

        // Get the number of messages
        var numParts = buf[pos++];
        var lengths = new int[numParts];

        // Get the lengths
        for (var i = 0; i < numParts; i++)
        {
            if (pos + 2 > buf.Length)
            {
                throw new ArgumentException("Truncated length", nameof(buf));
            }

            lengths[i] = BinaryPrimitives.ReadUInt16BigEndian(buf.AsSpan(pos));
            pos += 2;
        }

        // Get the parts
        for (var i = 0; i < numParts; i++)
        {
            var length = lengths[i];
            if (pos + length > buf.Length)
            {
                truncated = numParts - i;
                break;
            }

            var part = new byte[length];
            Buffer.BlockCopy(buf, pos, part, 0, length);
            parts.Add(part);
            pos += length;
        }

        return (truncated, parts);
    }

    #endregion

    #region Compression

    /// <summary>
    /// Compresses the payload using LZ4 compression
    /// </summary>
    public static byte[] CompressPayload(byte[] input)
    {
        var compressed = LZ4Pickler.Pickle(input);
        var msg = new Compress
        {
            Algo = CompressionType.Lz4,
            Buf = compressed
        };
        return Encode(MessageType.Compress, msg);
    }

    /// <summary>
    /// Decompresses the payload using the specified algorithm
    /// </summary>
    public static byte[] DecompressPayload(byte[] payload)
    {
        var msg = Decode<Compress>(payload);
        if(msg == null) throw new AggregateException("Decompression failed");
        return msg.Algo switch
        {
            CompressionType.None => msg.Buf,
            CompressionType.Lz4 => LZ4Pickler.Unpickle(msg.Buf),
            _ => throw new ArgumentException($"Unknown compression algorithm: {msg.Algo}")
        };
    }

    #endregion
}
