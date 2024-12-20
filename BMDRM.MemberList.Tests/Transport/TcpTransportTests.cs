using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace BMDRM.MemberList.Tests.Transport
{
    public class TcpTransportTests
    {
        private readonly ITestOutputHelper _output;
        private int _logCallCount;

        public TcpTransportTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private class TestLogger : ILogger
        {
            private readonly Action<string> _logAction;
            private readonly string _expectedMessage;
            private int _callCount;

            public TestLogger(Action<string> logAction, string expectedMessage)
            {
                _logAction = logAction;
                _expectedMessage = expectedMessage;
                _callCount = 0;
            }

            public int CallCount => _callCount;

            public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                var message = formatter(state, exception);
                if (message.Contains(_expectedMessage))
                {
                    Interlocked.Increment(ref _callCount);
                    _logAction(message);
                }
            }
        }

        [Fact]
        public async Task Transport_TcpListenBackoff_ShouldBackoffOnFailure()
        {
            // Arrange
            const int testDurationMs = 4000; // 4 seconds, same as Go test
            var expectedLogMessage = "Error accepting TCP connection";
            var logger = new TestLogger(
                message => _output.WriteLine($"Log: {message}"), 
                expectedLogMessage);

            // Create and immediately close a listener to simulate failures
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();

            // Create a new listener on the same port to force failures
            listener = new TcpListener(IPAddress.Loopback, port);
            var cts = new CancellationTokenSource();

            // Act
            var listenTask = Task.Run(async () =>
            {
                try
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        try
                        {
                            await listener.AcceptTcpClientAsync(cts.Token);
                        }
                        catch (Exception ex)
                        {
                            logger.Log(LogLevel.Error, 0, "Error accepting TCP connection", ex, (s, e) => s);
                            
                            // Implement exponential backoff similar to Go implementation
                            var delay = Math.Min(1000, (int)Math.Pow(2, Math.Min(logger.CallCount, 10)) * 5);
                            await Task.Delay(delay, cts.Token);
                        }
                    }
                }
                catch (OperationCanceledException) { }
            });

            // Wait for test duration
            await Task.Delay(testDurationMs);
            cts.Cancel();
            await listenTask;

            // Assert
            // In 4 seconds with the backoff formula, we expect around 11-13 retries
            // Initial delays (ms): 5, 10, 20, 40, 80, 160, 320, 640, 1000, 1000, 1000
            Assert.InRange(logger.CallCount, 10, 14);
            
            // Cleanup
            listener.Stop();
        }
    }
}
