using System.Threading.Channels;
using BMDRM.MemberList.State;

namespace BMDRM.MemberList.Delegates
{
    /// <summary>
    /// ChannelEventDelegate is used to enable an application to receive
    /// events about joins and leaves over a channel instead of a direct
    /// function call.
    /// 
    /// Care must be taken that events are processed in a timely manner from
    /// the channel, since this delegate will block until an event can be sent.
    /// </summary>
    public class ChannelEventDelegate : IEventDelegate
    {
        private readonly ChannelWriter<NodeEvent> _channel;

        public ChannelEventDelegate(ChannelWriter<NodeEvent> channel)
        {
            _channel = channel;
        }

        private static Node CloneNode(Node node)
        {
            return new Node
            {
                Name = node.Name,
                Address = node.Address,
                Meta = (byte[])node.Meta.Clone(),
                State = node.State,
                PMin = node.PMin,
                PMax = node.PMax,
                PCur = node.PCur,
                DMin = node.DMin,
                DMax = node.DMax,
                DCur = node.DCur
            };
        }

        public void NotifyJoin(Node node)
        {
            var copy = CloneNode(node);
            _channel.TryWrite(new NodeEvent(NodeEventType.NodeJoin, copy));
        }

        public void NotifyLeave(Node node)
        {
            var copy = CloneNode(node);
            _channel.TryWrite(new NodeEvent(NodeEventType.NodeLeave, copy));
        }

        public void NotifyUpdate(Node node)
        {
            var copy = CloneNode(node);
            _channel.TryWrite(new NodeEvent(NodeEventType.NodeUpdate, copy));
        }
    }
}
