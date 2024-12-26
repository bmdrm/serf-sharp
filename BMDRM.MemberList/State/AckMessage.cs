namespace BMDRM.MemberList.State
{
    public class AckMessage
    {
        public bool Complete { get; set; }
        public byte[] Payload { get; set; } = [];
        public DateTime Timestamp { get; set; }
    }
}
