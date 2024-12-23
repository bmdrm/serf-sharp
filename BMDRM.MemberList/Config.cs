using BMDRM.MemberList.Transport;

namespace BMDRM.MemberList
{
    public class Config
    {
        public string Name { get; set; } = "";
        public ITransport Transport { get; set; } = null!;
        public IDelegate Delegate { get; set; } = null!;
    }

    public interface IDelegate
    {
        void NotifyMsg(byte[] msg);
    }
}
