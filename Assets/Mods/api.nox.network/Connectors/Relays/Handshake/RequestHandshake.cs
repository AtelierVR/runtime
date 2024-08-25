using api.nox.network.Relays.Base;
using Nox.CCK;
using api.nox.network.Utils;

namespace api.nox.network.Relays.Handshakes
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