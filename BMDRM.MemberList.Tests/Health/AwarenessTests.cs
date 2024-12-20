using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BMDRM.MemberList.Health;
using Xunit;

namespace BMDRM.MemberList.Tests.Health
{
    public class AwarenessTests
    {
        [Fact]
        public void Constructor_ZeroMaxScore_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Awareness(0));
        }

        [Fact]
        public void Constructor_NegativeMaxScore_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Awareness(-1));
        }

        [Fact]
        public void Constructor_ValidMaxScore_InitializesWithZeroScore()
        {
            var awareness = new Awareness(5);
            Assert.Equal(0, awareness.Score);
            Assert.Equal(5, awareness.MaxScore);
        }

        [Fact]
        public void Constructor_WithMetricLabels_StoresLabels()
        {
            var labels = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" }
            };

            var awareness = new Awareness(5, labels);
            var storedLabels = awareness.MetricLabels;

            Assert.Equal(2, storedLabels.Count);
            Assert.Equal("value1", storedLabels["key1"]);
            Assert.Equal("value2", storedLabels["key2"]);
        }

        [Fact]
        public void ApplyDelta_PositiveDelta_IncreasesScore()
        {
            var awareness = new Awareness(5);
            var changed = awareness.ApplyDelta(2);

            Assert.True(changed);
            Assert.Equal(2, awareness.Score);
        }

        [Fact]
        public void ApplyDelta_NegativeDelta_DecreasesScore()
        {
            var awareness = new Awareness(5);
            awareness.ApplyDelta(3);
            var changed = awareness.ApplyDelta(-2);

            Assert.True(changed);
            Assert.Equal(1, awareness.Score);
        }

        [Fact]
        public void ApplyDelta_BelowZero_ClampsToZero()
        {
            var awareness = new Awareness(5);
            awareness.ApplyDelta(2);
            var changed = awareness.ApplyDelta(-3);

            Assert.True(changed);
            Assert.Equal(0, awareness.Score);
        }

        [Fact]
        public void ApplyDelta_AboveMax_ClampsToMaxMinusOne()
        {
            var awareness = new Awareness(5);
            var changed = awareness.ApplyDelta(10);

            Assert.True(changed);
            Assert.Equal(4, awareness.Score);
        }

        [Fact]
        public void ApplyDelta_NoChange_ReturnsFalse()
        {
            var awareness = new Awareness(5);
            awareness.ApplyDelta(4); // Score is now 4 (max-1)
            var changed = awareness.ApplyDelta(1); // Should not change

            Assert.False(changed);
            Assert.Equal(4, awareness.Score);
        }

        [Fact]
        public void ScaleTimeout_ZeroScore_ReturnsOriginalTimeout()
        {
            var awareness = new Awareness(5);
            var timeout = TimeSpan.FromSeconds(1);
            var scaled = awareness.ScaleTimeout(timeout);

            Assert.Equal(TimeSpan.FromSeconds(1), scaled);
        }

        [Fact]
        public void ScaleTimeout_WithScore_ScalesTimeout()
        {
            var awareness = new Awareness(5);
            awareness.ApplyDelta(2);
            var timeout = TimeSpan.FromSeconds(1);
            var scaled = awareness.ScaleTimeout(timeout);

            Assert.Equal(TimeSpan.FromSeconds(3), scaled);
        }

        [Fact]
        public async Task Score_ThreadSafety_HandlesParallelAccess()
        {
            var awareness = new Awareness(1000);
            var tasks = new List<Task>();

            // Create multiple tasks that modify the score
            for (int i = 0; i < 100; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    for (int j = 0; j < 10; j++)
                    {
                        awareness.ApplyDelta(1);
                        awareness.ApplyDelta(-1);
                    }
                }));
            }

            await Task.WhenAll(tasks);

            // After all operations, score should be 0
            Assert.Equal(0, awareness.Score);
        }

        [Fact]
        public void MetricLabels_Immutable_ReturnsCopy()
        {
            var labels = new Dictionary<string, string>
            {
                { "key", "value" }
            };

            var awareness = new Awareness(5, labels);
            var storedLabels = awareness.MetricLabels;

            // Modifying the original dictionary should not affect the stored labels
            labels["key"] = "newvalue";
            Assert.Equal("value", awareness.MetricLabels["key"]);

            // The returned dictionary should be immutable
            var dict = awareness.MetricLabels;
            Assert.Throws<NotSupportedException>(() =>
            {
                ((IDictionary<string, string>)dict).Add("newkey", "newvalue");
            });
        }
    }
}
