using System;
using System.Collections.Generic;
using System.Threading;

namespace BMDRM.MemberList.Suspicion
{
    /// <summary>
    /// Manages the suspect timer for a node and provides an interface to accelerate
    /// the timeout as we get more independent confirmations that a node is suspect.
    /// </summary>
    public class Suspicion : IDisposable
    {
        private int _confirmationCount;
        private readonly int _requiredConfirmations;
        private readonly TimeSpan _minTimeout;
        private readonly TimeSpan _maxTimeout;
        private readonly DateTime _startTime;
        private readonly Timer _timer;
        private readonly Action<int> _timeoutCallback;
        private readonly HashSet<string> _confirmations;
        private bool _disposed;

        /// <summary>
        /// Creates a new suspicion timer started with the max time, and that will drive
        /// to the min time after seeing k or more confirmations. The from node will be
        /// excluded from confirmations since we might get our own suspicion message
        /// gossiped back to us. The minimum time will be used if no confirmations are
        /// called for (k <= 0).
        /// </summary>
        /// <param name="from">Node to exclude from confirmations</param>
        /// <param name="requiredConfirmations">Number of confirmations needed to reach min timeout</param>
        /// <param name="minTimeout">Minimum timeout duration</param>
        /// <param name="maxTimeout">Maximum timeout duration</param>
        /// <param name="timeoutCallback">Callback function to execute on timeout</param>
        public Suspicion(string from, int requiredConfirmations, TimeSpan minTimeout, TimeSpan maxTimeout, Action<int> timeoutCallback)
        {
            _requiredConfirmations = requiredConfirmations;
            _minTimeout = minTimeout;
            _maxTimeout = maxTimeout;
            _timeoutCallback = timeoutCallback ?? throw new ArgumentNullException(nameof(timeoutCallback));
            _confirmations = new HashSet<string> { from };

            // If there aren't any confirmations to be made then take the min time from the start
            var timeout = requiredConfirmations < 1 ? minTimeout : maxTimeout;

            _startTime = DateTime.UtcNow;
            _timer = new Timer(OnTimeout, null, timeout, Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Confirms a suspicion from a given node. This will accelerate the timeout
        /// as more unique nodes confirm the same suspicion.
        /// </summary>
        /// <param name="from">Node confirming the suspicion</param>
        public void Confirm(string from)
        {
            if (_disposed)
            {
                return;
            }

            lock (_confirmations)
            {
                // Prevent double counting
                if (!_confirmations.Add(from))
                {
                    return;
                }

                var newCount = Interlocked.Increment(ref _confirmationCount);
                var elapsed = DateTime.UtcNow - _startTime;
                var remaining = CalculateRemainingSuspicionTime(newCount, _requiredConfirmations, elapsed, _minTimeout, _maxTimeout);

                // Reset the timer with the new timeout
                if (remaining <= TimeSpan.Zero)
                {
                    OnTimeout(null);
                }
                else
                {
                    _timer.Change(remaining, Timeout.InfiniteTimeSpan);
                }
            }
        }

        /// <summary>
        /// Calculates the remaining time to wait before considering a node dead.
        /// </summary>
        /// <param name="confirmations">Current number of confirmations</param>
        /// <param name="requiredConfirmations">Required number of confirmations</param>
        /// <param name="elapsed">Time elapsed since start</param>
        /// <param name="minTimeout">Minimum timeout duration</param>
        /// <param name="maxTimeout">Maximum timeout duration</param>
        /// <returns>Remaining time to wait</returns>
        public static TimeSpan CalculateRemainingSuspicionTime(int confirmations, int requiredConfirmations, TimeSpan elapsed, TimeSpan minTimeout, TimeSpan maxTimeout)
        {
            var n = Math.Max(0, confirmations);
            var k = Math.Max(1, requiredConfirmations);

            var frac = Math.Log(n + 1.0) / Math.Log(k + 1.0);
            var raw = maxTimeout.TotalSeconds - frac * (maxTimeout.TotalSeconds - minTimeout.TotalSeconds);
            var timeout = TimeSpan.FromMilliseconds(Math.Floor(1000.0 * raw));

            if (timeout < minTimeout)
            {
                timeout = minTimeout;
            }

            return timeout - elapsed;
        }

        private void OnTimeout(object? state)
        {
            if (!_disposed)
            {
                try
                {
                    _timeoutCallback(Interlocked.CompareExchange(ref _confirmationCount, 0, 0));
                }
                catch
                {
                    // Ignore any exceptions in the callback
                }
            }
        }

        /// <summary>
        /// Disposes the suspicion timer.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _timer.Dispose();
                _disposed = true;
            }
        }
    }
}
