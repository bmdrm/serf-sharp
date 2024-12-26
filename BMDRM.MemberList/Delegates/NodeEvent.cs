using BMDRM.MemberList.State;

namespace BMDRM.MemberList.Delegates
{
    /// <summary>
    /// NodeEvent is a single event related to node activity in the memberlist.
    /// The Node member of this struct must not be directly modified. If you wish 
    /// to modify the node, make a copy first.
    /// </summary>
    public class NodeEvent
    {
        /// <summary>
        /// The type of event that occurred
        /// </summary>
        public NodeEventType Event { get; set; }

        /// <summary>
        /// The node associated with the event
        /// </summary>
        public Node Node { get; set; } = null!;

        public NodeEvent(NodeEventType eventType, Node node)
        {
            Event = eventType;
            Node = node;
        }
    }
}
