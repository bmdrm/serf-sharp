using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading;

namespace BMDRM.MemberList.Health
{
    /// <summary>
    /// Manages a simple metric for tracking the estimated health of the local node.
    /// Health is primarily the node's ability to respond in the soft real-time manner
    /// required for correct health checking of other nodes in the cluster.
    /// </summary>
    public class Awareness
    {
        private static readonly Meter _meter = new("BMDRM.MemberList");
        private static readonly Histogram<int> _healthScore;

        private readonly object _lock = new();
        private readonly int _maxScore;
        private readonly IDictionary<string, string> _metricLabels;
        private int _score;

        static Awareness()
        {
            _healthScore = _meter.CreateHistogram<int>(
                "memberlist.health.score",
                description: "Current health score of the node. Lower values are healthier.");
        }

        /// <summary>
        /// Gets the current health score. Lower values are healthier and zero is the minimum value.
        /// </summary>
        public int Score
        {
            get
            {
                lock (_lock)
                {
                    return _score;
                }
            }
        }

        /// <summary>
        /// Gets the maximum possible health score (exclusive).
        /// </summary>
        public int MaxScore => _maxScore;

        /// <summary>
        /// Gets the metric labels used for emitting metrics.
        /// </summary>
        public IReadOnlyDictionary<string, string> MetricLabels => 
            new Dictionary<string, string>(_metricLabels).AsReadOnly();

        /// <summary>
        /// Initializes a new instance of the Awareness class.
        /// </summary>
        /// <param name="maxScore">The upper threshold for the timeout scale</param>
        /// <param name="metricLabels">Labels to put on all emitted metrics</param>
        public Awareness(int maxScore, IDictionary<string, string>? metricLabels = null)
        {
            if (maxScore <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxScore), "Max score must be greater than zero.");

            _maxScore = maxScore;
            _score = 0;
            _metricLabels = new Dictionary<string, string>(metricLabels ?? new Dictionary<string, string>());
        }

        /// <summary>
        /// Takes the given delta and applies it to the score in a thread-safe manner.
        /// It enforces a floor of zero and a max of maxScore - 1, so deltas may not
        /// change the overall score if it's railed at one of the extremes.
        /// </summary>
        /// <param name="delta">The change to apply to the score</param>
        /// <returns>True if the score was changed, false if it remained the same</returns>
        public bool ApplyDelta(int delta)
        {
            lock (_lock)
            {
                var initial = _score;
                _score = Math.Clamp(_score + delta, 0, _maxScore - 1);
                var final = _score;

                if (initial != final)
                {
                    EmitHealthMetric(final);
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Takes the given duration and scales it based on the current score.
        /// Less healthiness will lead to longer timeouts.
        /// </summary>
        /// <param name="timeout">The base timeout duration</param>
        /// <returns>The scaled timeout duration</returns>
        public TimeSpan ScaleTimeout(TimeSpan timeout)
        {
            var score = Score; // Thread-safe via property
            return timeout * (score + 1);
        }

        /// <summary>
        /// Emits a metric with the current health score.
        /// </summary>
        /// <param name="score">The current health score</param>
        private void EmitHealthMetric(int score)
        {
            // Record the health score with labels
            var tags = _metricLabels.Select(kvp => new KeyValuePair<string, object?>(kvp.Key, kvp.Value)).ToArray();
            _healthScore.Record(score, tags);
        }
    }
}
