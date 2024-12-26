namespace BMDRM.MemberList.Transport
{
    public class ConsoleLogger : ILogger
    {
        public void Log(string message)
        {
            Console.WriteLine($"{message}");
        }

        public void LogError(string message)
        {
            Console.Error.WriteLine(message);
        }
    }
}
