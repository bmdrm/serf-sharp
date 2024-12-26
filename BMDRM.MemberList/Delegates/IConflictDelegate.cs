using BMDRM.MemberList.State;

namespace BMDRM.MemberList.Delegates
{
    /// <summary>
    /// ConflictDelegate is used to inform a client that
    /// a node has attempted to join which would result in a
    /// name conflict. This happens if two clients are configured
    /// with the same name but different addresses.
    /// </summary>
    public interface IConflictDelegate
    {
        /// <summary>
        /// NotifyConflict is invoked when a name conflict is detected
        /// </summary>
        /// <param name="existing">The existing node with the conflicting name</param>
        /// <param name="other">The new node attempting to join with the same name</param>
        void NotifyConflict(Node existing, Node other);
    }
}
