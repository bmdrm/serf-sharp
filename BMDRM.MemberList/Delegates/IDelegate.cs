namespace BMDRM.MemberList.Delegates
{
    /// <summary>
    /// Delegate is the interface that clients must implement if they want to hook
    /// into the gossip layer of Memberlist. All the methods must be thread-safe,
    /// as they can and generally will be called concurrently.
    /// </summary>
    public interface IDelegate
    {
        /// <summary>
        /// NodeMeta is used to retrieve meta-data about the current node
        /// when broadcasting an alive message. Its length is limited to
        /// the given byte size. This metadata is available in the Node structure.
        /// </summary>
        /// <param name="limit">The maximum size of the metadata in bytes</param>
        /// <returns>Byte array containing the node metadata</returns>
        byte[] NodeMeta(int limit);

        /// <summary>
        /// NotifyMsg is called when a user-data message is received.
        /// Care should be taken that this method does not block, since doing
        /// so would block the entire UDP packet receive loop. Additionally, the byte
        /// array may be modified after the call returns, so it should be copied if needed
        /// </summary>
        /// <param name="message">The received message as a byte array</param>
        void NotifyMsg(byte[] message);

        /// <summary>
        /// GetBroadcasts is called when user data messages can be broadcast.
        /// It can return a list of buffers to send. Each buffer should assume an
        /// overhead as provided with a limit on the total byte size allowed.
        /// The total byte size of the resulting data to send must not exceed
        /// the limit. Care should be taken that this method does not block,
        /// since doing so would block the entire UDP packet receive loop.
        /// </summary>
        /// <param name="overhead">The overhead to consider for each message</param>
        /// <param name="limit">The maximum total size allowed</param>
        /// <returns>Array of byte arrays containing the broadcast messages</returns>
        byte[][] GetBroadcasts(int overhead, int limit);

        /// <summary>
        /// LocalState is used for a TCP Push/Pull. This is sent to
        /// the remote side in addition to the membership information. Any
        /// data can be sent here. See MergeRemoteState as well. The join
        /// parameter indicates this is for a join instead of a push/pull.
        /// </summary>
        /// <param name="join">Indicates if this is for a join operation</param>
        /// <returns>Byte array containing the local state</returns>
        byte[] LocalState(bool join);

        /// <summary>
        /// MergeRemoteState is invoked after a TCP Push/Pull. This is the
        /// state received from the remote side and is the result of the
        /// remote side's LocalState call. The join parameter indicates 
        /// this is for a join instead of a push/pull.
        /// </summary>
        /// <param name="buffer">The received remote state as a byte array</param>
        /// <param name="join">Indicates if this is for a join operation</param>
        void MergeRemoteState(byte[] buffer, bool join);
    }
}
