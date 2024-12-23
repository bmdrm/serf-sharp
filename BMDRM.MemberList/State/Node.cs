using System.Net;

namespace BMDRM.MemberList.State
{
    public class Node
    {
        public string Name { get; set; } = "";
        public IPEndPoint Address { get; set; } = null!;
        public byte[] Meta { get; set; } = Array.Empty<byte>(); // Metadata from the delegate for this node
        public NodeStateType State { get; set; } = NodeStateType.Alive;
        public short PMin { get; set; } // Minimum protocol version this understands
        public short PMax { get; set; } // Maximum protocol version this understands
        public short PCur { get; set; } // Current version node is speaking
        public short DMin { get; set; } // Min protocol version for the delegate to understand
        public short DMax { get; set; } // Max protocol version for the delegate to understand
        public short DCur { get; set; } // Current version delegate is speaking

        /// <summary>
        /// Returns the host:port form of a node's address, suitable for use with a transport.
        /// </summary>
        public virtual string GetAddress()
        {
            return Address.ToString();
        }

        /// <summary>
        /// Returns the node name and host:port form of a node's address, suitable for use with a transport.
        /// </summary>
        public virtual (string Addr, string Name) GetFullAddress()
        {
            return (Address.ToString(), Name);
        }

        /// <summary>
        /// Returns the node name
        /// </summary>
        public override string ToString()
        {
            return Name;
        }
    }
}
