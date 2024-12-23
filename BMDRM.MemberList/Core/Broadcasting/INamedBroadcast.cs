namespace BMDRM.MemberList.Core.Broadcasting
{
    /// <summary>
    /// NamedBroadcast is an optional extension of the Broadcast interface that
    /// gives each message a unique string name, and that is used to optimize
    /// </summary>
    public interface INamedBroadcast : IBroadcast
    {
        /// <summary>
        /// The unique identity of this broadcast message.
        /// </summary>
        string Name { get; }
    }
}
