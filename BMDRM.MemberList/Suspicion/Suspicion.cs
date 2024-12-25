// Copyright (c) HashiCorp, Inc.
// SPDX-License-Identifier: MPL-2.0

using System;
using System.Collections.Concurrent;
using System.Threading;

namespace BMDRM.MemberList.Suspicion
{
    /// <summary>
    /// Suspicion manages the suspect timer for a node and provides an interface
    /// to accelerate the timeout as we get more independent confirmations that
    /// a node is suspect.
    /// </summary>
    public sealed class Suspicion : IDisposable
    {
        // n is the number of independent confirmations we've seen. This must
        // be updated using atomic instructions to prevent contention with the
        // timer callback.
        private int _confirmationCount;

        // k is the number of independent confirmations we'd like to see in
        // order to drive the timer to its minimum value.
        private readonly int _targetConfirmations;

        // min is the minimum timer value.
        private readonly TimeSpan _minDuration;

        // max is the maximum timer value.
        private readonly TimeSpan _maxDuration;

        // start captures the timestamp when we began the timer. This is used
        // so we can calculate durations to feed the timer during updates in
        // a way that achieves the overall time we'd like.
        private readonly DateTime _startTime;

        // timer is the underlying timer that implements the timeout.
        private readonly Timer _timer;

        // timeoutFn is the function to call when the timer expires.
        private readonly Action<int> _timeoutCallback;

        // confirmations is a map of "from" nodes that have confirmed a given
        // node is suspect. This prevents double counting.
        private readonly ConcurrentDictionary<string, byte> _confirmations;

        private bool _disposed;

        /// <summary>
        /// Creates a new suspicion timer started with the max time, and that will drive
        /// to the min time after seeing k or more confirmations. The from node will be
        // excluded from confirmations since we might get our own suspicion message
        /// gossiped back to us. The minimum time will be used if no confirmations are
        /// called for (k <= 0).
        /// </summary>
        /// <param name="from">The source node to exclude from confirmations</param>
        /// <param name="targetConfirmations">Number of confirmations needed to reach minimum time</param>
        /// <param name="minDuration">Minimum timer duration</param>
        /// <param name="maxDuration">Maximum timer duration</param>
        /// <param name="timeoutCallback">Callback function when timer expires</param>
        public Suspicion(string from, int targetConfirmations, TimeSpan minDuration, TimeSpan maxDuration, Action<int> timeoutCallback)
        {
            _targetConfirmations = targetConfirmations;
            _minDuration = minDuration;
            _maxDuration = maxDuration;
            _timeoutCallback = timeoutCallback ?? throw new ArgumentNullException(nameof(timeoutCallback));
            _confirmations = new ConcurrentDictionary<string, byte>();

            // Exclude the from node from any confirmations
            _confirmations.TryAdd(from, 0);

            // If there aren't any confirmations to be made then take the min
            // time from the start
            var timeout = maxDuration;
            if (targetConfirmations < 1)
            {
                timeout = minDuration;
                // Set confirmation count to 0 since we don't expect any confirmations
                Interlocked.Exchange(ref _confirmationCount, 0);
            }

            _timer = new Timer(OnTimerCallback, null, timeout, Timeout.InfiniteTimeSpan);
            _startTime = DateTime.UtcNow;
        }

        private void OnTimerCallback(object? state)
        {
            _timeoutCallback(Interlocked.CompareExchange(ref _confirmationCount, 0, 0));
        }

        /// <summary>
        /// Calculates the remaining time to wait before considering a node dead.
        /// The return value can be negative, so be prepared to fire the timer immediately in that case.
        /// </summary>
        private static TimeSpan CalculateRemainingSuspicionTime(int confirmationCount, int targetConfirmations, 
            TimeSpan elapsed, TimeSpan minDuration, TimeSpan maxDuration)
        {
            // If we have no target confirmations, return the minimum duration
            if (targetConfirmations <= 0)
            {
                return minDuration - elapsed;
            }

            // Calculate the fraction of confirmations we have received
            var fraction = Math.Log(confirmationCount + 1.0) / Math.Log(targetConfirmations + 1.0);
            var raw = maxDuration.TotalSeconds - fraction * (maxDuration.TotalSeconds - minDuration.TotalSeconds);
            var timeout = TimeSpan.FromMilliseconds(Math.Floor(raw * 1000.0));

            // If we've reached the target confirmations or the calculated timeout is less than min,
            // use the minimum duration
            if (confirmationCount >= targetConfirmations || timeout < minDuration)
            {
                timeout = minDuration;
            }

            // We have to take into account the amount of time that has passed so
            // far, so we get the right overall timeout.
            return timeout - elapsed;
        }

        /// <summary>
        /// Registers that a possibly new peer has also determined the given
        /// node is suspect. This returns true if this was new information, and false
        /// if it was a duplicate confirmation, or if we've got enough confirmations to
        /// hit the minimum.
        /// </summary>
        /// <param name="from">The node confirming the suspicion</param>
        /// <returns>True if this was new information, false otherwise</returns>
        public bool Confirm(string from)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(Suspicion));
            }

            // If we've got enough confirmations then stop accepting them
            if (Interlocked.CompareExchange(ref _confirmationCount, 0, 0) >= _targetConfirmations)
            {
                return false;
            }

            // Only allow one confirmation from each possible peer
            if (!_confirmations.TryAdd(from, 0))
            {
                return false;
            }

            // Increment the confirmation count
            var newCount = Interlocked.Increment(ref _confirmationCount);

            // Compute the new timeout given the current number of confirmations and
            // adjust the timer. If the timeout becomes negative and we can cleanly
            // stop the timer then we will call the timeout function directly.
            var elapsed = DateTime.UtcNow - _startTime;
            var remaining = CalculateRemainingSuspicionTime(newCount, _targetConfirmations, elapsed, _minDuration, _maxDuration);

            // If the timeout becomes negative, try to stop the timer and call the timeout function
            if (remaining <= TimeSpan.Zero)
            {
                // Stop the timer first
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
                ThreadPool.QueueUserWorkItem(_ => _timeoutCallback(newCount));
            }
            else
            {
                // Try to stop the timer and reset it with the new timeout
                _timer.Change(remaining, Timeout.InfiniteTimeSpan);
            }

            return true;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _timer.Dispose();
        }
    }
}
