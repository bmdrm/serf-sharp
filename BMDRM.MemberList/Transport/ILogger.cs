namespace BMDRM.MemberList.Transport
{
    public interface ILogger
    {
        void Log(string message);
        void LogError(string message);
    }
}
