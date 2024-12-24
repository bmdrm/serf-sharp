namespace BMDRM.MemberList.Core.Broadcasting
{
    /// <summary>
    /// Represents a broadcast with limited transmissions
    /// </summary>
    internal class LimitedBroadcast
    {
        public int Transmits { get; set; }  // Number of transmissions attempted
        public long MessageLength { get; }   // Length of the broadcast message
        public long Id { get; }              // Unique incrementing id stamped at submission time
        public IBroadcast Broadcast { get; }
        public string? Name { get; }         // Set if Broadcast is a NamedBroadcast

        public LimitedBroadcast(IBroadcast broadcast, int transmits, long id)
        {
            Broadcast = broadcast;
            Transmits = transmits;
            MessageLength = broadcast.Message().Length;
            Id = id;
            
            if (broadcast is INamedBroadcast named)
            {
                Name = named.Name;
            }
        }
    }
}
