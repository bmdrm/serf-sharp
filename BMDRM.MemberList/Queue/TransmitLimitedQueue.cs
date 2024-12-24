using BMDRM.MemberList.Core.Broadcasting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BMDRM.MemberList.Queue
{
    /// <summary>
    /// TransmitLimitedQueue is used to queue messages to broadcast to
    /// the cluster (via gossip) but limits the number of transmits per
    /// message. It also prioritizes messages with lower transmit counts.
    /// </summary>
    public class TransmitLimitedQueue
    {
        /// <summary>
        /// NumNodes returns the number of nodes in the cluster.
        /// Used to determine the retransmit count (log2-based).
        /// </summary>
        public Func<int> NumNodes { get; set; }

        /// <summary>
        /// RetransmitMult is the multiplier used to determine the maximum
        /// number of retransmissions attempted.
        /// </summary>
        public int RetransmitMult { get; set; }

        private readonly object _lock = new object();
        private readonly SortedSet<LimitedBroadcast> _queue = new(new LimitedBroadcastComparer());
        private Dictionary<string, LimitedBroadcast> _namedBroadcasts = new();
        private long _idGen;

        public TransmitLimitedQueue()
        {
            NumNodes = () => 1;
            RetransmitMult = 2;
        }

        /// <summary>
        /// QueueBroadcast is used to enqueue a broadcast with initial transmit count = 0.
        /// </summary>
        public void QueueBroadcast(IBroadcast broadcast)
        {
            QueueBroadcastInternal(broadcast, 0);
        }

        private void QueueBroadcastInternal(IBroadcast broadcast, int initialTransmits)
        {
            lock (_lock)
            {
                if (_idGen == long.MaxValue)
                {
                    _idGen = 1;
                }
                else
                {
                    _idGen++;
                }

                var lb = new LimitedBroadcast(broadcast, initialTransmits, _idGen);

                if (!string.IsNullOrEmpty(lb.Name))
                {
                    if (_namedBroadcasts.TryGetValue(lb.Name, out var old))
                    {
                        old.Broadcast.Finished();
                        DeleteItemLocked(old);
                    }
                }
                else if (!(broadcast is IUniqueBroadcast))
                {
                    var toRemove = new List<LimitedBroadcast>();
                    foreach (var item in _queue)
                    {
                        if (item.Broadcast is INamedBroadcast) continue;
                        if (item.Broadcast is IUniqueBroadcast) continue;

                        if (broadcast.Invalidates(item.Broadcast))
                        {
                            item.Broadcast.Finished();
                            toRemove.Add(item);
                        }
                    }
                    foreach (var item in toRemove)
                    {
                        DeleteItemLocked(item);
                    }
                }

                AddItemLocked(lb);
            }
        }

        private void DeleteItemLocked(LimitedBroadcast lb)
        {
            _queue.Remove(lb);
            if (!string.IsNullOrEmpty(lb.Name))
            {
                _namedBroadcasts.Remove(lb.Name);
            }
            if (_queue.Count == 0)
            {
                _idGen = 0;
            }
        }

        private void AddItemLocked(LimitedBroadcast lb)
        {
            _queue.Add(lb);
            if (!string.IsNullOrEmpty(lb.Name))
            {
                _namedBroadcasts[lb.Name] = lb;
            }
        }

        /// <summary>
        /// GetBroadcasts is used to get up to 'limit' bytes worth of broadcasts
        /// </summary>
        public byte[][] GetBroadcasts(int overhead, int limit)
        {
            lock (_lock)
            {
                if (_queue.Count == 0)
                {
                    return Array.Empty<byte[]>();
                }

                int retransmitLimit = ComputeRetransmitLimit(NumNodes());
                var toSend = new List<byte[]>();
                var reinsert = new List<LimitedBroadcast>();
                var toRemove = new List<LimitedBroadcast>();
                int bytesUsed = 0;

                // Create a snapshot of the queue to avoid modification during iteration
                var snapshot = _queue.ToList();
                foreach (var item in snapshot)
                {
                    // Check if we have space for this message
                    long freeSpace = (long)(limit - bytesUsed);
                    if (freeSpace <= overhead || item.MessageLength > freeSpace - overhead)
                    {
                        continue;
                    }

                    // Add message to send
                    var msg = item.Broadcast.Message();
                    bytesUsed += overhead + msg.Length;
                    toSend.Add(msg);

                    // Mark for removal
                    toRemove.Add(item);

                    // Check retransmit limit
                    if (item.Transmits + 1 >= retransmitLimit)
                    {
                        item.Broadcast.Finished();
                    }
                    else
                    {
                        // Increment transmit count and reinsert later
                        item.Transmits++;
                        reinsert.Add(item);
                    }
                }

                // Remove and reinsert items
                foreach (var item in toRemove)
                {
                    DeleteItemLocked(item);
                }
                foreach (var item in reinsert)
                {
                    AddItemLocked(item);
                }

                return toSend.ToArray();
            }
        }

        /// <summary>
        /// Clears all queued messages
        /// </summary>
        public void Reset()
        {
            lock (_lock)
            {
                foreach (var item in _queue)
                {
                    item.Broadcast.Finished();
                }
                _queue.Clear();
                _namedBroadcasts.Clear();
                _idGen = 0;
            }
        }

        /// <summary>
        /// Returns the number of queued messages
        /// </summary>
        public int NumQueued()
        {
            lock (_lock)
            {
                return _queue.Count;
            }
        }

        /// <summary>
        /// Prune will retain the maxRetain latest messages
        /// </summary>
        public void Prune(int maxRetain)
        {
            lock (_lock)
            {
                if (_queue.Count <= maxRetain)
                {
                    return;
                }

                var toDelete = _queue.Count - maxRetain;
                var toRemove = _queue
                    .OrderByDescending(x => x.Id)
                    .Skip(maxRetain)
                    .ToList();

                foreach (var item in toRemove)
                {
                    DeleteItemLocked(item);
                }
            }
        }

        private static int ComputeRetransmitLimit(int numNodes)
        {
            if (numNodes <= 1) return 1;
            return (int)Math.Ceiling(Math.Log10(numNodes + 1)) * 1;
        }

        private class LimitedBroadcastComparer : IComparer<LimitedBroadcast>
        {
            public int Compare(LimitedBroadcast? x, LimitedBroadcast? y)
            {
                if (x == null || y == null)
                {
                    return x == null ? (y == null ? 0 : -1) : 1;
                }

                // Primary sort by transmits (ascending)
                var transmitCompare = x.Transmits.CompareTo(y.Transmits);
                if (transmitCompare != 0)
                {
                    return transmitCompare;
                }

                // Secondary sort by message length (descending)
                var lengthCompare = y.MessageLength.CompareTo(x.MessageLength);
                if (lengthCompare != 0)
                {
                    return lengthCompare;
                }

                // Tertiary sort by ID (descending)
                return y.Id.CompareTo(x.Id);
            }
        }
    }
}
