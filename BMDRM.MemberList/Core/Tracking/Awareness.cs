using System.Diagnostics.Metrics;

namespace BMDRM.MemberList.Core.Tracking
{
    /// <summary>
    /// Manages a simple metric for tracking the estimated health of the local node.
    /// Health is primarily the node's ability to respond in the soft real-time manner
    /// required for correct health checking of other nodes in the cluster.
    /// </summary>
    public class Awareness
    {
        private readonly int _max;
        private int _score;  // Will be accessed via Interlocked
        private readonly KeyValuePair<string, string>[] _metricLabels;
        private readonly ObservableGauge<int> _healthScoreGauge;

        public Awareness(int max, KeyValuePair<string, string>[] metricLabels)
        {
            _max = max;
            _score = 0;
            _metricLabels = metricLabels;
            var meter = new Meter("BMDRM.MemberList");
            _healthScoreGauge = meter.CreateObservableGauge("memberlist.health.score", 
                GetHealthScore,
                description: "Current health score of the node");
        }

        /// <summary>
        /// Takes the given delta and applies it to the score using atomic operations.
        /// It enforces a floor of zero and a max of max-1.
        /// </summary>
        public void ApplyDelta(int delta)
        {
            int initial;
            int final;

            do
            {
                initial = Interlocked.CompareExchange(ref _score, 0, 0); // Read current value
                var newValue = initial + delta;
                
                // Constrain the new value
                if (newValue < 0)
                    newValue = 0;
                else if (newValue > (_max - 1))
                    newValue = _max - 1;
                
                final = Interlocked.CompareExchange(ref _score, newValue, initial);
                
            } while (final != initial); // Retry if another thread modified the value
        }

        /// <summary>
        /// Returns the raw health score.
        /// </summary>
        public int GetHealthScore()
        {
            return Interlocked.CompareExchange(ref _score, 0, 0); // Atomic read
        }

        /// <summary>
        /// Takes the given duration and scales it based on the current score.
        /// Less healthiness will lead to longer timeouts.
        /// </summary>
        public TimeSpan ScaleTimeout(TimeSpan timeout)
        {
            var score = GetHealthScore();
            return timeout * (score + 1);
        }
    }
}
