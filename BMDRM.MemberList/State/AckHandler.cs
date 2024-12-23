using System.Timers;

namespace BMDRM.MemberList.State
{
    /// <summary>
    /// Used to register handlers for incoming acks and nacks.
    /// </summary>
    public class AckHandler
    {
        public Action<byte[], DateTime>? AckFn { get; set; }
        public Action? NackFn { get; set; }
        public System.Timers.Timer? Timer { get; set; }
    }
}
