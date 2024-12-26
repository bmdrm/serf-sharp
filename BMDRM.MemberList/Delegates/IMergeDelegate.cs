using BMDRM.MemberList.State;

namespace BMDRM.MemberList.Delegates
{
    /// <summary>
    /// MergeDelegate is used to involve a client in
    /// a potential cluster merge operation. Namely, when
    /// a node does a TCP push/pull (as part of a join),
    /// the delegate is involved and allowed to cancel the join
    /// based on custom logic. The merge delegate is NOT invoked
    /// as part of the push-pull anti-entropy.
    /// </summary>
    public interface IMergeDelegate
    {
        /// <summary>
        /// NotifyMerge is invoked when a merge could take place.
        /// Provides a list of the nodes known by the peer. If
        /// the return value is non-null, the merge is canceled.
        /// </summary>
        /// <param name="peers">List of nodes known by the peer</param>
        /// <returns>Exception if the merge should be canceled, null otherwise</returns>
        Exception? NotifyMerge(IReadOnlyList<Node> peers);
    }
}
