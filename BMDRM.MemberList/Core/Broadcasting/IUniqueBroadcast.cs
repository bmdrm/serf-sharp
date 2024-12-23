namespace BMDRM.MemberList.Core.Broadcasting
{
    /// <summary>
    /// UniqueBroadcast is an optional interface that indicates that each message is
    /// intrinsically unique and there is no need to scan the broadcast queue for duplicates.
    /// </summary>
    public interface IUniqueBroadcast : IBroadcast
    {
        /// <summary>
        /// UniqueBroadcast is just a marker method for this interface.
        /// </summary>
        void UniqueBroadcast();
    }
}
