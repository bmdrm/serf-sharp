namespace BMDRM.MemberList.State
{
    /// <summary>
    /// Used to indicate a 'ping' packet was successfully issued but no response was received
    /// </summary>
    public class NoPingResponseException : Exception
    {
        public string Node { get; }

        public NoPingResponseException(string node)
            : base($"No response from node {node}")
        {
            Node = node;
        }
    }
}
