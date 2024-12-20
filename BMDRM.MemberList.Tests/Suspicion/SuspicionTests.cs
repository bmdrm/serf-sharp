using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace BMDRM.MemberList.Tests.Suspicion
{
    public class SuspicionTests
    {
        [Fact]
        public async Task Constructor_NoConfirmationsNeeded_FiresWithMinTimeout()
        {
            var timeoutFired = false;
            var confirmationCount = -1;

            using var suspicion = new MemberList.Suspicion.Suspicion(
                "node1",
                0, // no confirmations needed
                TimeSpan.FromMilliseconds(10),
                TimeSpan.FromMilliseconds(100),
                count =>
                {
                    timeoutFired = true;
                    confirmationCount = count;
                });

            // Wait a bit longer than the min timeout
            await Task.Delay(20);

            Assert.True(timeoutFired);
            Assert.Equal(0, confirmationCount);
        }

        [Fact]
        public async Task Constructor_ConfirmationsNeeded_StartsWithMaxTimeout()
        {
            var timeoutFired = false;

            using var suspicion = new MemberList.Suspicion.Suspicion(
                "node1",
                2, // confirmations needed
                TimeSpan.FromMilliseconds(10),
                TimeSpan.FromMilliseconds(100),
                _ => timeoutFired = true);

            // Wait longer than min but shorter than max timeout
            await Task.Delay(50);

            Assert.False(timeoutFired);
        }

        [Fact]
        public async Task Confirm_SameNodeMultipleTimes_CountsOnlyOnce()
        {
            var tcs = new TaskCompletionSource<int>();

            using var suspicion = new MemberList.Suspicion.Suspicion(
                "node1",
                3,
                TimeSpan.FromMilliseconds(50), // Short timeout
                TimeSpan.FromMilliseconds(50), // Same as min timeout
                count => tcs.TrySetResult(count));

            // Confirm from the same node multiple times
            suspicion.Confirm("node2");
            suspicion.Confirm("node2");
            suspicion.Confirm("node2");

            // Wait for the callback to be called
            var count = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1));
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task Confirm_EnoughConfirmations_AcceleratesTimeout()
        {
            var timeoutFired = false;
            var confirmationCount = -1;

            using var suspicion = new MemberList.Suspicion.Suspicion(
                "node1",
                2, // need 2 confirmations
                TimeSpan.FromMilliseconds(10),
                TimeSpan.FromMilliseconds(1000),
                count =>
                {
                    timeoutFired = true;
                    confirmationCount = count;
                });

            // Add confirmations
            suspicion.Confirm("node2");
            suspicion.Confirm("node3");

            // Wait a bit longer than the min timeout
            await Task.Delay(20);

            Assert.True(timeoutFired);
            Assert.Equal(2, confirmationCount);
        }

        [Fact]
        public void CalculateRemainingSuspicionTime_NoConfirmations_ReturnsMaxTimeout()
        {
            var minTimeout = TimeSpan.FromSeconds(1);
            var maxTimeout = TimeSpan.FromSeconds(10);
            var elapsed = TimeSpan.Zero;

            var remaining = MemberList.Suspicion.Suspicion.CalculateRemainingSuspicionTime(
                0, 3, elapsed, minTimeout, maxTimeout);

            Assert.Equal(maxTimeout, remaining);
        }

        [Fact]
        public void CalculateRemainingSuspicionTime_AllConfirmations_ReturnsMinTimeout()
        {
            var minTimeout = TimeSpan.FromSeconds(1);
            var maxTimeout = TimeSpan.FromSeconds(10);
            var elapsed = TimeSpan.Zero;

            var remaining = MemberList.Suspicion.Suspicion.CalculateRemainingSuspicionTime(
                3, 3, elapsed, minTimeout, maxTimeout);

            Assert.Equal(minTimeout, remaining);
        }

        [Fact]
        public void CalculateRemainingSuspicionTime_PartialConfirmations_ReturnsIntermediateTimeout()
        {
            var minTimeout = TimeSpan.FromSeconds(1);
            var maxTimeout = TimeSpan.FromSeconds(10);
            var elapsed = TimeSpan.Zero;

            var remaining = MemberList.Suspicion.Suspicion.CalculateRemainingSuspicionTime(
                1, 3, elapsed, minTimeout, maxTimeout);

            Assert.True(remaining > minTimeout);
            Assert.True(remaining < maxTimeout);
        }

        [Fact]
        public void CalculateRemainingSuspicionTime_WithElapsed_SubtractsElapsedTime()
        {
            var minTimeout = TimeSpan.FromSeconds(1);
            var maxTimeout = TimeSpan.FromSeconds(10);
            var elapsed = TimeSpan.FromSeconds(2);

            var remaining = MemberList.Suspicion.Suspicion.CalculateRemainingSuspicionTime(
                0, 3, elapsed, minTimeout, maxTimeout);

            Assert.Equal(maxTimeout - elapsed, remaining);
        }

        [Fact]
        public void Dispose_MultipleCalls_OnlyDisposesOnce()
        {
            var suspicion = new MemberList.Suspicion.Suspicion(
                "node1",
                2,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(10),
                _ => { });

            // Should not throw
            suspicion.Dispose();
            suspicion.Dispose();
        }

        [Fact]
        public void Confirm_AfterDispose_DoesNotThrow()
        {
            var suspicion = new MemberList.Suspicion.Suspicion(
                "node1",
                2,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(10),
                _ => { });

            suspicion.Dispose();

            // Should not throw
            suspicion.Confirm("node2");
        }
    }
}
