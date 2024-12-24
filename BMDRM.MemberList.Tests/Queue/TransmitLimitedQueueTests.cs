using BMDRM.MemberList.Core.Broadcasting;
using BMDRM.MemberList.Queue;
using Xunit;

namespace BMDRM.MemberList.Tests.Queue;

public class TransmitLimitedQueueTests
{
    private class TestBroadcast : IBroadcast
    {
        private readonly byte[] _message;
        private bool _finished;

        public TestBroadcast(byte[] message)
        {
            _message = message;
        }

        public bool Invalidates(IBroadcast broadcast)
        {
            return false;
        }

        public byte[] Message()
        {
            return _message;
        }

        public void Finished()
        {
            _finished = true;
        }

        public bool IsFinished()
        {
            return _finished;
        }
    }

    private class NamedTestBroadcast : INamedBroadcast
    {
        private readonly byte[] _message;
        private readonly string _name;
        private bool _finished;

        public NamedTestBroadcast(byte[] message, string name)
        {
            _message = message;
            _name = name;
        }

        public bool Invalidates(IBroadcast broadcast)
        {
            if (broadcast is not INamedBroadcast named)
            {
                return false;
            }
            return _name == named.Name;
        }

        public byte[] Message()
        {
            return _message;
        }

        public void Finished()
        {
            _finished = true;
        }

        public bool IsFinished()
        {
            return _finished;
        }

        public string Name => _name;
    }

    [Fact]
    public void TestQueueBroadcast()
    {
        var q = new TransmitLimitedQueue
        {
            NumNodes = () => 3,
            RetransmitMult = 1
        };

        // Queue a message
        var b1 = new TestBroadcast(new byte[] { 1 });
        q.QueueBroadcast(b1);
        Assert.Equal(1, q.NumQueued());

        // Queue the same message
        q.QueueBroadcast(b1);
        Assert.Equal(2, q.NumQueued());

        // Queue message with invalidation
        var b2 = new TestBroadcast(new byte[] { 2 });
        q.QueueBroadcast(b2);
        Assert.Equal(3, q.NumQueued());
    }

    [Fact]
    public void TestQueueBroadcast_Named()
    {
        var q = new TransmitLimitedQueue
        {
            NumNodes = () => 3,
            RetransmitMult = 1
        };

        // Queue a message
        var b1 = new NamedTestBroadcast(new byte[] { 1 }, "test");
        q.QueueBroadcast(b1);
        Assert.Equal(1, q.NumQueued());

        // Queue the same message, should replace
        var b2 = new NamedTestBroadcast(new byte[] { 2 }, "test");
        q.QueueBroadcast(b2);
        Assert.Equal(1, q.NumQueued());
        Assert.True(b1.IsFinished());

        // Queue message with different name
        var b3 = new NamedTestBroadcast(new byte[] { 3 }, "test2");
        q.QueueBroadcast(b3);
        Assert.Equal(2, q.NumQueued());
    }

    [Fact]
    public void TestGetBroadcasts()
    {
        var q = new TransmitLimitedQueue
        {
            NumNodes = () => 3,
            RetransmitMult = 1
        };

        // Queue a few messages
        var b1 = new TestBroadcast(new byte[] { 1 });
        var b2 = new TestBroadcast(new byte[] { 2 });
        q.QueueBroadcast(b1);
        q.QueueBroadcast(b2);

        // Get broadcasts with enough space
        var broadcasts = q.GetBroadcasts(0, 10);
        Assert.Equal(2, broadcasts.Length);
        Assert.Equal(new byte[] { 1 }, broadcasts[0]);
        Assert.Equal(new byte[] { 2 }, broadcasts[1]);

        // Get broadcasts with limited space
        broadcasts = q.GetBroadcasts(0, 1);
        Assert.Single(broadcasts);
        Assert.Equal(new byte[] { 1 }, broadcasts[0]);
    }

    [Fact]
    public void TestGetBroadcasts_RetransmitLimit()
    {
        var q = new TransmitLimitedQueue
        {
            NumNodes = () => 3,
            RetransmitMult = 1
        };

        // Queue a message
        var b1 = new TestBroadcast(new byte[] { 1 });
        q.QueueBroadcast(b1);

        // Get broadcasts multiple times
        // With 3 nodes, retransmit limit should be 1 * ceil(log10(4)) = 1
        // So message should be transmitted only once
        var broadcasts = q.GetBroadcasts(0, 10);
        Assert.Single(broadcasts);
        Assert.Equal(new byte[] { 1 }, broadcasts[0]);

        // Message should be gone
        var finalBroadcasts = q.GetBroadcasts(0, 10);
        Assert.Empty(finalBroadcasts);
        Assert.True(b1.IsFinished());
    }

    [Fact]
    public void TestPrune()
    {
        var q = new TransmitLimitedQueue
        {
            NumNodes = () => 3,
            RetransmitMult = 1
        };

        // Queue a lot of messages
        for (var i = 0; i < 10; i++)
        {
            q.QueueBroadcast(new TestBroadcast(new byte[] { (byte)i }));
        }

        // Prune to 5
        q.Prune(5);
        Assert.Equal(5, q.NumQueued());

        // Get broadcasts should work
        var broadcasts = q.GetBroadcasts(0, 10);
        Assert.Equal(5, broadcasts.Length);
    }
}
