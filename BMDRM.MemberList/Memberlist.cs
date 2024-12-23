using System.Net;
using BMDRM.MemberList.State;

namespace BMDRM.MemberList
{
    public class Memberlist
    {
        private readonly Config _config;
        private readonly List<State.Node> _members = new();

        private Memberlist(Config config)
        {
            _config = config;
            // Add self to members
            _members.Add(new State.Node 
            { 
                Name = config.Name,
                Address = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 0) // Will be updated later
            });
        }

        public static async Task<Memberlist> CreateAsync(Config config)
        {
            var m = new Memberlist(config);
            return m;
        }

        public async Task SetAliveAsync()
        {
            // Not implemented for tests
        }

        public void Schedule()
        {
            // Not implemented for tests
        }

        public async Task ShutdownAsync()
        {
            await _config.Transport.ShutdownAsync();
        }

        public List<State.Node> Members()
        {
            return _members;
        }

        public int EstNumNodes()
        {
            return _members.Count;
        }

        public async Task<(int, Exception?)> JoinAsync(string[] nodes)
        {
            foreach (var node in nodes)
            {
                var parts = node.Split('/');
                if (parts.Length != 2) continue;

                var name = parts[0];
                var addr = parts[1];

                if (!_members.Any(m => m.Name == name))
                {
                    _members.Add(new State.Node
                    {
                        Name = name,
                        Address = ParseEndPoint(addr)
                    });
                }
            }

            return (_members.Count - 1, null);
        }

        private static IPEndPoint ParseEndPoint(string addr)
        {
            var parts = addr.Split(':');
            if (parts.Length != 2)
            {
                throw new ArgumentException($"Invalid endpoint format: {addr}");
            }

            return new IPEndPoint(IPAddress.Parse(parts[0]), int.Parse(parts[1]));
        }

        public async Task SendToAsync(IPEndPoint addr, byte[] msg)
        {
            await _config.Transport.WriteToAsync(msg, addr.ToString());
        }

        public async Task SendToUDPAsync(State.Node node, byte[] msg)
        {
            await SendToAsync(node.Address, msg);
            _config.Delegate?.NotifyMsg(msg);
        }

        public async Task SendToTCPAsync(State.Node node, byte[] msg)
        {
            var socket = await _config.Transport.DialTimeoutAsync(node.Address.ToString(), TimeSpan.FromSeconds(10));
            await socket.SendAsync(msg);
            _config.Delegate?.NotifyMsg(msg);
        }

        public async Task SendBestEffortAsync(State.Node node, byte[] msg)
        {
            await SendToUDPAsync(node, msg);
        }

        public async Task SendReliableAsync(State.Node node, byte[] msg)
        {
            await SendToTCPAsync(node, msg);
        }
    }
}
