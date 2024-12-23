using System.Net;

namespace BMDRM.MemberList.State
{
    /// <summary>
    /// Used to manage our state view of another node
    /// </summary>
    public class NodeState : Node
    {
        public uint Incarnation { get; set; }  // Last known incarnation number
        public new NodeStateType State { get; set; }  // Current state
        public DateTime StateChange { get; set; }  // Time last state change happened

        /// <summary>
        /// Returns the host:port form of a node's address, suitable for use with a transport.
        /// </summary>
        public override string GetAddress()
        {
            return base.GetAddress();
        }

        /// <summary>
        /// Returns the node name and host:port form of a node's address, suitable for use with a transport.
        /// </summary>
        public override (string Addr, string Name) GetFullAddress()
        {
            return base.GetFullAddress();
        }

        /// <summary>
        /// Returns true if the node is in either Dead or Left state
        /// </summary>
        public bool DeadOrLeft()
        {
            return State == NodeStateType.Dead || State == NodeStateType.Left;
        }
    }
}
