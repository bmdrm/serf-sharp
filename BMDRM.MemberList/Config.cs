using BMDRM.MemberList.Transport;
using BMDRM.MemberList.Delegates;

namespace BMDRM.MemberList
{
    public class Config
    {
        public string Name { get; set; } = "";
        public ITransport Transport { get; set; } = null!;
        public IDelegate Delegate { get; set; } = null!;
    }

}
