using BMDRM.MemberList.State;

namespace BMDRM.MemberList.Delegates
{
    /// <summary>
    /// EventDelegate is a simpler delegate that is used only to receive
    /// notifications about members joining and leaving. The methods in this
    /// delegate may be called by multiple threads, but never concurrently.
    /// This allows you to reason about ordering.
    /// </summary>
    public interface IEventDelegate
    {
        /// <summary>
        /// NotifyJoin is invoked when a node is detected to have joined.
        /// The Node argument must not be modified.
        /// </summary>
        /// <param name="node">The node that joined</param>
        void NotifyJoin(Node node);

        /// <summary>
        /// NotifyLeave is invoked when a node is detected to have left.
        /// The Node argument must not be modified.
        /// </summary>
        /// <param name="node">The node that left</param>
        void NotifyLeave(Node node);

        /// <summary>
        /// NotifyUpdate is invoked when a node is detected to have
        /// updated, usually involving the metadata. The Node argument
        /// must not be modified.
        /// </summary>
        /// <param name="node">The node that was updated</param>
        void NotifyUpdate(Node node);
    }
}
