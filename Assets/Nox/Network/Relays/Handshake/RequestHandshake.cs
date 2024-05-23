using Nox.Network.Relays.Base;
using Nox.Scripts;
using Nox.CCK;

namespace Nox.Network.Relays.Handshakes
{
    public class RequestHandshake : RelayRequest
    {
        public ushort ProtocolVersion;
        public Engine Engine;
        public Platfrom Platform;

        public override Buffer ToBuffer()
        {
            var buffer = new Buffer();
            buffer.Write(ProtocolVersion);
            buffer.Write(Engine);
            buffer.Write(Platform);
            return buffer;
        }
    }
}