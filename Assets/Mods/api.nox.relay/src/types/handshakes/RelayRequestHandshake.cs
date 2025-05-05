using Nox.CCK.Utils;

namespace api.nox.relay.types.Handshakes
{
    public class RequestHandshake : Request
    {
        public ushort ProtocolVersion;
        public Engine Engine;
        public Platform Platform;

        public override Buffer ToBuffer()
        {
            var buffer = new Buffer();
            buffer.Write(ProtocolVersion);
            buffer.Write(Engine.GetEngineName());
            buffer.Write(Platform.GetPlatformName());
            return buffer;
        }
    }
}