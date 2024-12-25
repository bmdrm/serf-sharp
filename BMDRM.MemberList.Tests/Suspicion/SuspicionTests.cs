// Copyright (c) HashiCorp, Inc.
// SPDX-License-Identifier: MPL-2.0

using System;
using System.Threading;
using System.Threading.Tasks;
using BMDRM.MemberList.Suspicion;
using Xunit;

namespace BMDRM.MemberList.Tests.Suspicion
{
    public class SuspicionTests
    {
        private static readonly TimeSpan Fudge = TimeSpan.FromMilliseconds(25);

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

            void OnTimeout(int count)
            {
                confirmationCount = count;
                fired.TrySetResult(DateTime.UtcNow - start);
            }

            // Act
            using var suspicion = new global::BMDRM.MemberList.Suspicion.Suspicion(from, k, min, max, OnTimeout);
            
            var seenConfirmations = new HashSet<string>();
            foreach (var confirmer in confirmations)
            {
                await Task.Delay(Fudge);
                var wasNew = suspicion.Confirm(confirmer);
                var shouldBeNew = confirmer != from && !seenConfirmations.Contains(confirmer);
                seenConfirmations.Add(confirmer);
                Assert.Equal(shouldBeNew, wasNew);
            }

            // Wait until right before the timeout
            var alreadyWaited = TimeSpan.FromMilliseconds(confirmations.Length * Fudge.TotalMilliseconds);
            await Task.Delay(expected - alreadyWaited - Fudge);
            
            // Should not have fired yet
            Assert.False(fired.Task.IsCompleted);

            // Wait through the timeout
            await Task.Delay(2 * Fudge);
            
            // Should have fired
            var result = await fired.Task.WaitAsync(TimeSpan.FromMilliseconds(100));
            Assert.Equal(expectedConfirmations, confirmationCount);

            // Confirm after to ensure it doesn't fire again
            suspicion.Confirm("late");
            await Task.Delay(expected + 2 * Fudge);
            
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
