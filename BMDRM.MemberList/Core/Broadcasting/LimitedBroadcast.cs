namespace BMDRM.MemberList.Core.Broadcasting
{
    /// <summary>
    /// Represents a broadcast with limited transmissions
    /// </summary>
    internal class LimitedBroadcast : IComparable<LimitedBroadcast>
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

        public int CompareTo(LimitedBroadcast? other)
        {
            if (other == null) return 1;

            // Primary sort by transmits (ascending)
            var transmitCompare = Transmits.CompareTo(other.Transmits);
            if (transmitCompare != 0) return transmitCompare;

            // Secondary sort by message length (descending)
            var lengthCompare = other.MessageLength.CompareTo(MessageLength);
            if (lengthCompare != 0) return lengthCompare;

            // Tertiary sort by ID (descending)
            return other.Id.CompareTo(Id);
        }
    }
}
