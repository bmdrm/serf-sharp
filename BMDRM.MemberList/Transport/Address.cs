namespace BMDRM.MemberList.Transport
{
    /// <summary>
    /// Represents a network address with an optional node name.
    /// </summary>
    public class Address
    {
        /// <summary>
        /// Network address in "host:port" format.
        /// </summary>
        public string Addr { get; set; }

        /// <summary>
        /// Optional name of the node being addressed.
        /// </summary>
        public string Name { get; set; }

        public override string ToString()
        {
            return !string.IsNullOrEmpty(Name) 
                ? $"{Name} ({Addr})" 
                : Addr;
        }
    }
}
