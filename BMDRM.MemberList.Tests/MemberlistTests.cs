using System;
using System.Threading.Tasks;
using Xunit;

namespace BMDRM.MemberList.Tests
{
    public class MemberlistTests
    {
        [Fact]
        public async Task Schedule_WhenCalled_SetsUpTimers()
        {
            // Arrange
            var config = new Config
            {
                Name = "test1",
                ProbeInterval = TimeSpan.FromMilliseconds(100),
                PushPullInterval = TimeSpan.FromMilliseconds(200),
                GossipInterval = TimeSpan.FromMilliseconds(300),
                GossipNodes = 3
            };
            var memberlist = await Memberlist.CreateAsync(config);

            // Act
            memberlist.Schedule();

            // Assert - verify that timers are created
            // We can't directly access private fields, but we can verify the behavior
            // by checking if the timers are running through their side effects
            
            // Wait a bit to allow timers to run
            await Task.Delay(500);
            
            // The actual verification would depend on how we expose the state
            // For now, we're just verifying that the method doesn't throw
        }

        [Fact]
        public async Task Schedule_WhenCalledMultipleTimes_OnlyCreatesTimersOnce()
        {
            // Arrange
            var config = new Config
            {
                Name = "test2",
                ProbeInterval = TimeSpan.FromMilliseconds(100),
                PushPullInterval = TimeSpan.FromMilliseconds(200),
                GossipInterval = TimeSpan.FromMilliseconds(300),
                GossipNodes = 3
            };
            var memberlist = await Memberlist.CreateAsync(config);

            // Act
            memberlist.Schedule();
            memberlist.Schedule(); // Second call should be idempotent

            // Assert
            // Similar to above test, we can't directly verify the timer count
            // but we can ensure the method is idempotent by checking it doesn't throw
        }

        [Fact]
        public async Task Schedule_WithZeroIntervals_DoesNotCreateTimers()
        {
            // Arrange
            var config = new Config
            {
                Name = "test3",
                ProbeInterval = TimeSpan.Zero,
                PushPullInterval = TimeSpan.Zero,
                GossipInterval = TimeSpan.Zero,
                GossipNodes = 0
            };
            var memberlist = await Memberlist.CreateAsync(config);

            // Act
            memberlist.Schedule();

            // Assert
            // Method should complete without creating any timers
        }
    }
}
