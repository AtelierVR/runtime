using Nox.Network.Relays.Base;
using Nox.Scripts;

namespace Nox.Network.Relays.Auth
{
    public class RequestAuth : RelayRequest
    {
        public string AccessToken;
        public string ServerAddress;
        public uint UserId;
        public AuthFlags Flags;

        public override Buffer ToBuffer()
        {
            var buffer = new Buffer();
            buffer.Write((byte)Flags);
            buffer.Write(UserId);
            buffer.Write(ServerAddress);
            buffer.Write(AccessToken);
            return buffer;
        }
    }
}