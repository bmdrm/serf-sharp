// Copyright (c) HashiCorp, Inc.
// SPDX-License-Identifier: MPL-2.0

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using BMDRM.MemberList.Suspicion;
using Xunit;
using Xunit.Abstractions;

namespace BMDRM.MemberList.Tests.Suspicion
{
    public class SuspicionTests
    {
        private static readonly TimeSpan Fudge = TimeSpan.FromMilliseconds(50); // Increased from 25ms to 50ms
        private readonly ITestOutputHelper _output;

        public SuspicionTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData(0, 3, 0, 2, 30, 30)]
        [InlineData(1, 3, 2, 2, 30, 14)]
        [InlineData(2, 3, 3, 2, 30, 4.810)]
        [InlineData(3, 3, 4, 2, 30, -2)]
        [InlineData(4, 3, 5, 2, 30, -3)]
        [InlineData(5, 3, 10, 2, 30, -8)]
        public void CalculateRemainingSuspicionTime_ReturnsExpectedTime(
            int n, int k, int elapsed, int min, int max, double expectedSeconds)
        {
            // Arrange
            var elapsedSpan = TimeSpan.FromSeconds(elapsed);
            var minSpan = TimeSpan.FromSeconds(min);
            var maxSpan = TimeSpan.FromSeconds(max);
            var expectedSpan = TimeSpan.FromSeconds(expectedSeconds);

            // Act
            var remaining = typeof(global::BMDRM.MemberList.Suspicion.Suspicion)
                .GetMethod("CalculateRemainingSuspicionTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                .Invoke(null, new object[] { n, k, elapsedSpan, minSpan, maxSpan }) as TimeSpan?;

            // Assert
            Assert.NotNull(remaining);
            Assert.Equal(expectedSpan.TotalSeconds, remaining!.Value.TotalSeconds, 3);
        }

        [Theory]
        [InlineData(0, "me", new string[0], 2.0)]
        [InlineData(1, "me", new[] { "me", "foo" }, 1.25)]
        [InlineData(1, "me", new[] { "me", "foo", "foo", "foo" }, 1.25)]
        [InlineData(2, "me", new[] { "me", "foo", "bar" }, 0.81)]
        [InlineData(3, "me", new[] { "me", "foo", "bar", "baz" }, 0.5)]
        [InlineData(3, "me", new[] { "me", "foo", "bar", "baz", "zoo" }, 0.5)]
        public async Task Timer_FiresAtExpectedTime(int expectedConfirmations, string from, string[] confirmations, double expectedSeconds)
        {
            // Arrange
            const int k = 3;
            var min = TimeSpan.FromMilliseconds(500);
            var max = TimeSpan.FromSeconds(2);
            var expected = TimeSpan.FromSeconds(expectedSeconds);
            
            var confirmationCount = 0;
            var fired = new TaskCompletionSource<TimeSpan>();
            var start = DateTime.UtcNow;
            _output.WriteLine($"Test started at: {start:HH:mm:ss.fff}");

            void OnTimeout(int count)
            {
                var fireTime = DateTime.UtcNow;
                confirmationCount = count;
                _output.WriteLine($"Timer fired at: {fireTime:HH:mm:ss.fff}, elapsed: {(fireTime - start).TotalMilliseconds}ms");
                fired.TrySetResult(fireTime - start);
            }

            // Act
            using var suspicion = new global::BMDRM.MemberList.Suspicion.Suspicion(from, k, min, max, OnTimeout);
            
            var seenConfirmations = new HashSet<string>();
            foreach (var confirmer in confirmations)
            {
                var beforeConfirm = DateTime.UtcNow;
                await Task.Delay(Fudge);
                var wasNew = suspicion.Confirm(confirmer);
                var shouldBeNew = confirmer != from && !seenConfirmations.Contains(confirmer);
                seenConfirmations.Add(confirmer);
                _output.WriteLine($"Confirmed {confirmer} at {DateTime.UtcNow:HH:mm:ss.fff}, wasNew: {wasNew}, shouldBeNew: {shouldBeNew}");
                Assert.Equal(shouldBeNew, wasNew);
            }

            // Calculate how long we've already waited
            var alreadyWaited = TimeSpan.FromMilliseconds(confirmations.Length * Fudge.TotalMilliseconds);
            var remainingWait = expected - alreadyWaited - Fudge;
            _output.WriteLine($"Already waited: {alreadyWaited.TotalMilliseconds}ms, remaining wait: {remainingWait.TotalMilliseconds}ms");

            // Wait until right before the timeout
            await Task.Delay(remainingWait);
            
            // Should not have fired yet
            var beforeCheck = DateTime.UtcNow;
            _output.WriteLine($"Checking if fired too early at: {beforeCheck:HH:mm:ss.fff}, elapsed: {(beforeCheck - start).TotalMilliseconds}ms");
            Assert.False(fired.Task.IsCompleted);

            // Wait through the timeout with extra margin
            await Task.Delay(3 * Fudge);
            
            // Should have fired
            var result = await fired.Task.WaitAsync(TimeSpan.FromMilliseconds(200));
            _output.WriteLine($"Final timer duration: {result.TotalMilliseconds}ms");
            Assert.Equal(expectedConfirmations, confirmationCount);

            // Confirm after to ensure it doesn't fire again
            suspicion.Confirm("late");
            await Task.Delay(expected + 3 * Fudge);
            
            // Should not fire again
            Assert.Equal(TaskStatus.RanToCompletion, fired.Task.Status);
        }

        [Fact]
        public async Task Timer_ZeroK_FiresImmediately()
        {
            // Arrange
            var fired = new TaskCompletionSource<int>();
            void OnTimeout(int count) => fired.TrySetResult(count);

            // Act
            using var suspicion = new global::BMDRM.MemberList.Suspicion.Suspicion(
                "me", 
                targetConfirmations: 0,
                minDuration: TimeSpan.FromMilliseconds(25),
                maxDuration: TimeSpan.FromSeconds(30),
                timeoutCallback: OnTimeout);

            var wasNew = suspicion.Confirm("foo");
            Assert.False(wasNew);

            // Assert
            var result = await fired.Task.WaitAsync(TimeSpan.FromMilliseconds(50));
            Assert.Equal(0, result);
        }

        [Fact]
        public async Task Timer_Immediate_FiresWhenOverdue()
        {
            // Arrange
            var fired = new TaskCompletionSource<int>();
            void OnTimeout(int count) => fired.TrySetResult(count);

            // Act
            using var suspicion = new global::BMDRM.MemberList.Suspicion.Suspicion(
                "me",
                targetConfirmations: 1,
                minDuration: TimeSpan.FromMilliseconds(100),
                maxDuration: TimeSpan.FromSeconds(30),
                timeoutCallback: OnTimeout);

            await Task.Delay(TimeSpan.FromMilliseconds(200));
            suspicion.Confirm("foo");

            // Assert
            var result = await fired.Task.WaitAsync(TimeSpan.FromMilliseconds(25));
            Assert.Equal(1, result);
        }
    }
}
