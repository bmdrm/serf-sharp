using System.Net;

namespace BMDRM.MemberList.State
{
    public class Node
    {
        public string Name { get; set; } = "";
        public IPEndPoint Address { get; set; } = null!;
        public byte[] Meta { get; set; } = Array.Empty<byte>(); // Metadata from the delegate for this node
        public NodeStateType State { get; set; } = NodeStateType.Alive;
        public byte PMin { get; set; } // Minimum protocol version this understands
        public byte PMax { get; set; } // Maximum protocol version this understands
        public byte PCur { get; set; } // Current version node is speaking
        public byte DMin { get; set; } // Min protocol version for the delegate to understand
        public byte DMax { get; set; } // Max protocol version for the delegate to understand
        public byte DCur { get; set; } // Current version delegate is speaking

        public override string ToString()
        {
            return Name;
        }
    }
}
