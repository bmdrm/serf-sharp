using BMDRM.MemberList.State;

namespace BMDRM.MemberList.Delegates
{
    /// <summary>
    /// AliveDelegate is used to involve a client in processing
    /// a node "alive" message. When a node joins, either through
    /// a UDP gossip or TCP push/pull, we update the state of
    /// that node via an alive message. This can be used to filter
    /// a node out and prevent it from being considered a peer
    /// using application specific logic.
    /// </summary>
    public interface IAliveDelegate
    {
        /// <summary>
        /// NotifyAlive is invoked when a message about a live
        /// node is received from the network. Returning a non-null
        /// exception prevents the node from being considered a peer.
        /// </summary>
        /// <param name="peer">The node that is alive</param>
        /// <returns>Exception if the node should not be considered a peer, null otherwise</returns>
        Exception? NotifyAlive(Node peer);
    }
}
