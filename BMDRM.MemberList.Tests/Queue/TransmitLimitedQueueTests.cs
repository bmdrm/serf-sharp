using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Xunit;
using BMDRM.MemberList.Core.Broadcasting;
using BMDRM.MemberList.Queue;

namespace BMDRM.MemberList.Tests.Queue
{
    public class TransmitLimitedQueueTests
    {
        private class TestBroadcast : IBroadcast
        {
            private readonly byte[] _message;
            private readonly EventWaitHandle _finishedSignal;

            public TestBroadcast(byte[] message, EventWaitHandle finishedSignal = null)
            {
                _message = message;
                _finishedSignal = finishedSignal;
            }

            public bool Invalidates(IBroadcast other) => false;
            public byte[] Message() => _message;
            public void Finished() => _finishedSignal?.Set();
        }

        private class NamedTestBroadcast : TestBroadcast, INamedBroadcast
        {
            private readonly string _name;

            public NamedTestBroadcast(byte[] message, string name, EventWaitHandle finishedSignal = null)
                : base(message, finishedSignal)
            {
                _name = name;
            }

            public string Name => _name;
        }

        private class TestLb
        {
            public int Transmits { get; set; }
            public long MsgLen { get; set; }
            public long Id { get; set; }
        }

        private class TestLbComparer : IComparer<TestLb>
        {
            public static readonly TestLbComparer Instance = new TestLbComparer();

            public int Compare(TestLb x, TestLb y)
            {
                if (x == null && y == null) return 0;
                if (x == null) return -1;
                if (y == null) return 1;

                if (x.Transmits < y.Transmits) return -1;
                if (x.Transmits > y.Transmits) return 1;

                // Compare msgLen descending
                if (x.MsgLen > y.MsgLen) return -1;
                if (x.MsgLen < y.MsgLen) return 1;

                // Compare id descending
                if (x.Id > y.Id) return -1;
                if (x.Id < y.Id) return 1;

                return 0;
            }
        }

        [Fact]
        public void TestLimitedBroadcastLess()
        {
            var testCases = new[]
            {
                new
                {
                    Name = "diff-transmits",
                    A = new TestLb { Transmits = 0, MsgLen = 10, Id = 100 },
                    B = new TestLb { Transmits = 1, MsgLen = 10, Id = 100 },
                },
                new
                {
                    Name = "same-transmits--diff-len",
                    A = new TestLb { Transmits = 0, MsgLen = 12, Id = 100 },
                    B = new TestLb { Transmits = 0, MsgLen = 10, Id = 100 },
                },
                new
                {
                    Name = "same-transmits--same-len--diff-id",
                    A = new TestLb { Transmits = 0, MsgLen = 12, Id = 100 },
                    B = new TestLb { Transmits = 0, MsgLen = 12, Id = 90 },
                },
            };

            foreach (var tc in testCases)
            {
                Assert.True(TestLbComparer.Instance.Compare(tc.A, tc.B) < 0);

                var set = new SortedSet<TestLb>(TestLbComparer.Instance);
                set.Add(tc.B);
                set.Add(tc.A);

                var min = set.Min;
                Assert.Equal(tc.A.Transmits, min.Transmits);
                Assert.Equal(tc.A.MsgLen, min.MsgLen);
                Assert.Equal(tc.A.Id, min.Id);

                var max = set.Max;
                Assert.Equal(tc.B.Transmits, max.Transmits);
                Assert.Equal(tc.B.MsgLen, max.MsgLen);
                Assert.Equal(tc.B.Id, max.Id);
            }
        }

        [Fact]
        public void TestTransmitLimited_Queue()
        {
            var q = new TransmitLimitedQueue
            {
                RetransmitMult = 1,
                NumNodes = () => 1
            };

            q.QueueBroadcast(new NamedTestBroadcast(new byte[] { }, "test"));
            q.QueueBroadcast(new NamedTestBroadcast(new byte[] { }, "foo"));
            q.QueueBroadcast(new NamedTestBroadcast(new byte[] { }, "bar"));

            Assert.Equal(3, q.NumQueued());

            q.QueueBroadcast(new NamedTestBroadcast(new byte[] { }, "test"));

            Assert.Equal(3, q.NumQueued());
        }

        [Fact]
        public void TestTransmitLimited_GetBroadcasts()
        {
            var q = new TransmitLimitedQueue
            {
                RetransmitMult = 3,
                NumNodes = () => 10
            };

            var msg1 = new byte[] { 
                (byte)'1',(byte)'.',(byte)' ',(byte)'t',(byte)'h',(byte)'i',(byte)'s',(byte)' ',
                (byte)'i',(byte)'s',(byte)' ',(byte)'a',(byte)' ',(byte)'t',(byte)'e',(byte)'s',(byte)'t',(byte)'.'
            };
            var msg2 = new byte[] {
                (byte)'2',(byte)'.',(byte)' ',(byte)'t',(byte)'h',(byte)'i',(byte)'s',(byte)' ',
                (byte)'i',(byte)'s',(byte)' ',(byte)'a',(byte)' ',(byte)'t',(byte)'e',(byte)'s',(byte)'t',(byte)'.'
            };
            var msg3 = new byte[] {
                (byte)'3',(byte)'.',(byte)' ',(byte)'t',(byte)'h',(byte)'i',(byte)'s',(byte)' ',
                (byte)'i',(byte)'s',(byte)' ',(byte)'a',(byte)' ',(byte)'t',(byte)'e',(byte)'s',(byte)'t',(byte)'.'
            };
            var msg4 = new byte[] {
                (byte)'4',(byte)'.',(byte)' ',(byte)'t',(byte)'h',(byte)'i',(byte)'s',(byte)' ',
                (byte)'i',(byte)'s',(byte)' ',(byte)'a',(byte)' ',(byte)'t',(byte)'e',(byte)'s',(byte)'t',(byte)'.'
            };

            q.QueueBroadcast(new NamedTestBroadcast(msg1, "test"));
            q.QueueBroadcast(new NamedTestBroadcast(msg2, "foo"));
            q.QueueBroadcast(new NamedTestBroadcast(msg3, "bar"));
            q.QueueBroadcast(new NamedTestBroadcast(msg4, "baz"));

            var all = q.GetBroadcasts(2, 80);
            Assert.Equal(4, all.Length);

            var partial = q.GetBroadcasts(3, 80);
            Assert.Equal(3, partial.Length);
        }

        [Fact]
        public void TestTransmitLimited_GetBroadcasts_Limit()
        {
            var q = new TransmitLimitedQueue
            {
                RetransmitMult = 1,
                NumNodes = () => 10
            };

            var msg1 = new byte[] { 
                (byte)'1',(byte)'.',(byte)' ',(byte)'t',(byte)'h',(byte)'i',(byte)'s',(byte)' ',
                (byte)'i',(byte)'s',(byte)' ',(byte)'a',(byte)' ',(byte)'t',(byte)'e',(byte)'s',(byte)'t',(byte)'.'
            };
            var msg2 = new byte[] {
                (byte)'2',(byte)'.',(byte)' ',(byte)'t',(byte)'h',(byte)'i',(byte)'s',(byte)' ',
                (byte)'i',(byte)'s',(byte)' ',(byte)'a',(byte)' ',(byte)'t',(byte)'e',(byte)'s',(byte)'t',(byte)'.'
            };
            var msg3 = new byte[] {
                (byte)'3',(byte)'.',(byte)' ',(byte)'t',(byte)'h',(byte)'i',(byte)'s',(byte)' ',
                (byte)'i',(byte)'s',(byte)' ',(byte)'a',(byte)' ',(byte)'t',(byte)'e',(byte)'s',(byte)'t',(byte)'.'
            };
            var msg4 = new byte[] {
                (byte)'4',(byte)'.',(byte)' ',(byte)'t',(byte)'h',(byte)'i',(byte)'s',(byte)' ',
                (byte)'i',(byte)'s',(byte)' ',(byte)'a',(byte)' ',(byte)'t',(byte)'e',(byte)'s',(byte)'t',(byte)'.'
            };

            q.QueueBroadcast(new NamedTestBroadcast(msg1, "test"));
            q.QueueBroadcast(new NamedTestBroadcast(msg2, "foo"));
            q.QueueBroadcast(new NamedTestBroadcast(msg3, "bar"));
            q.QueueBroadcast(new NamedTestBroadcast(msg4, "baz"));

            var partial1 = q.GetBroadcasts(3, 80);
            Assert.Equal(3, partial1.Length);

            var partial2 = q.GetBroadcasts(3, 80);
            Assert.Equal(3, partial2.Length);

            var partial3 = q.GetBroadcasts(3, 80);
            Assert.Equal(2, partial3.Length);

            var partial4 = q.GetBroadcasts(3, 80);
            Assert.Empty(partial4);
        }

        [Fact]
        public void TestTransmitLimited_Prune()
        {
            var q = new TransmitLimitedQueue
            {
                RetransmitMult = 1,
                NumNodes = () => 10
            };

            var ch1 = new ManualResetEvent(false);
            var ch2 = new ManualResetEvent(false);

            var msg1 = new byte[] { (byte)'1',(byte)'.',(byte)' ',(byte)'t',(byte)'e',(byte)'s',(byte)'t',(byte)'.' };
            var msg2 = new byte[] { (byte)'2',(byte)'.',(byte)' ',(byte)'t',(byte)'e',(byte)'s',(byte)'t',(byte)'.' };
            var msg3 = new byte[] { (byte)'3',(byte)'.',(byte)' ',(byte)'t',(byte)'e',(byte)'s',(byte)'t',(byte)'.' };
            var msg4 = new byte[] { (byte)'4',(byte)'.',(byte)' ',(byte)'t',(byte)'e',(byte)'s',(byte)'t',(byte)'.' };

            q.QueueBroadcast(new TestBroadcast(msg1, ch1));
            q.QueueBroadcast(new TestBroadcast(msg2, ch2));
            q.QueueBroadcast(new TestBroadcast(msg3));
            q.QueueBroadcast(new TestBroadcast(msg4));

            q.Prune(2);
            Assert.Equal(2, q.NumQueued());

            Assert.True(ch1.WaitOne(0), "expected test broadcast to be finished");
            Assert.True(ch2.WaitOne(0), "expected foo broadcast to be finished");
        }
    }
}
