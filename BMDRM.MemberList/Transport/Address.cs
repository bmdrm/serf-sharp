namespace BMDRM.MemberList.Transport
{
    /// <summary>
    /// Address represents a network endpoint with an optional name.
    /// </summary>
    public class Address
    {
        /// <summary>
        /// Addr is a network address as a string, similar to Socket.Connect. This usually is
        /// in the form of "host:port". This is required.
        /// </summary>
        public string Addr { get; }

        /// <summary>
        /// Name is the name of the node being addressed. This is optional but
        /// transports may require it.
        /// </summary>
        public string? Name { get; }

        public Address(string addr, string? name = null)
        {
            Addr = addr;
            Name = name;
        }

        public override string ToString()
        {
            return !string.IsNullOrEmpty(Name) ? $"{Name} ({Addr})" : Addr;
        }
    }
}
