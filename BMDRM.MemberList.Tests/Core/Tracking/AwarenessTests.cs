using System.Diagnostics.Metrics;
using BMDRM.MemberList.Core.Tracking;
using Xunit;

namespace BMDRM.MemberList.Tests.Core.Tracking
{
    public class AwarenessTests
    {
        public class TestCase
        {
            public int Delta { get; set; }
            public int ExpectedScore { get; set; }
            public TimeSpan ExpectedTimeout { get; set; }
        }

        [Fact]
        public void TestAwareness()
        {
            // Arrange
            var testCases = new[]
            {
                new TestCase { Delta = 0, ExpectedScore = 0, ExpectedTimeout = TimeSpan.FromSeconds(1) },
                new TestCase { Delta = -1, ExpectedScore = 0, ExpectedTimeout = TimeSpan.FromSeconds(1) },
                new TestCase { Delta = -10, ExpectedScore = 0, ExpectedTimeout = TimeSpan.FromSeconds(1) },
                new TestCase { Delta = 1, ExpectedScore = 1, ExpectedTimeout = TimeSpan.FromSeconds(2) },
                new TestCase { Delta = -1, ExpectedScore = 0, ExpectedTimeout = TimeSpan.FromSeconds(1) },
                new TestCase { Delta = 10, ExpectedScore = 7, ExpectedTimeout = TimeSpan.FromSeconds(8) },
                new TestCase { Delta = -1, ExpectedScore = 6, ExpectedTimeout = TimeSpan.FromSeconds(7) },
                new TestCase { Delta = -1, ExpectedScore = 5, ExpectedTimeout = TimeSpan.FromSeconds(6) },
                new TestCase { Delta = -1, ExpectedScore = 4, ExpectedTimeout = TimeSpan.FromSeconds(5) },
                new TestCase { Delta = -1, ExpectedScore = 3, ExpectedTimeout = TimeSpan.FromSeconds(4) },
                new TestCase { Delta = -1, ExpectedScore = 2, ExpectedTimeout = TimeSpan.FromSeconds(3) },
                new TestCase { Delta = -1, ExpectedScore = 1, ExpectedTimeout = TimeSpan.FromSeconds(2) },
                new TestCase { Delta = -1, ExpectedScore = 0, ExpectedTimeout = TimeSpan.FromSeconds(1) },
                new TestCase { Delta = -1, ExpectedScore = 0, ExpectedTimeout = TimeSpan.FromSeconds(1) }
            };

            var awareness = new Awareness(8, Array.Empty<KeyValuePair<string, string>>());

            // Act & Assert
            for (int i = 0; i < testCases.Length; i++)
            {
                var testCase = testCases[i];
                
                awareness.ApplyDelta(testCase.Delta);
                
                var actualScore = awareness.GetHealthScore();
                Assert.Equal(testCase.ExpectedScore, actualScore);
                
                var actualTimeout = awareness.ScaleTimeout(TimeSpan.FromSeconds(1));
                Assert.Equal(testCase.ExpectedTimeout, actualTimeout);
            }
        }
    }
}
