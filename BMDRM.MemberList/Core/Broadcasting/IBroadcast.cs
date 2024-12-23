namespace BMDRM.MemberList.Core.Broadcasting
{
    /// <summary>
    /// Broadcast is something that can be broadcasted via gossip to the memberlist cluster.
    /// </summary>
    public interface IBroadcast
    {
        /// <summary>
        /// Invalidates checks if enqueuing the current broadcast invalidates a previous broadcast
        /// </summary>
        bool Invalidates(IBroadcast broadcast);

        /// <summary>
        /// Returns a byte form of the message
        /// </summary>
        byte[] Message();

        /// <summary>
        /// Finished is invoked when the message will no longer be broadcast, 
        /// either due to invalidation or to the transmit limit being reached
        /// </summary>
        void Finished();
    }
}
