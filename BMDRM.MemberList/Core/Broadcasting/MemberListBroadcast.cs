namespace BMDRM.MemberList.Core.Broadcasting
{
    /// <summary>
    /// Represents a broadcast message for the memberlist system.
    /// Implements both IBroadcast and INamedBroadcast interfaces.
    /// </summary>
    public class MemberListBroadcast : INamedBroadcast
    {
        private readonly string _node;
        private readonly byte[] _message;
        private readonly Action? _notify;

        public MemberListBroadcast(string node, byte[] message, Action? notify = null)
        {
            _node = node;
            _message = message;
            _notify = notify;
        }

        public bool Invalidates(IBroadcast other)
        {
            // Check if that broadcast is a memberlist type
            if (other is MemberListBroadcast mb)
            {
                // Invalidates any message about the same node
                return _node == mb._node;
            }
            return false;
        }

        public string Name => _node;

        public byte[] Message()
        {
            return _message;
        }

        public void Finished()
        {
            _notify?.Invoke();
        }
    }
}
