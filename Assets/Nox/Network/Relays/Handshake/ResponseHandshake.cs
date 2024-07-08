using System.Net;
using Nox.Network.Relays.Base;
using Nox.Scripts;

namespace Nox.Network.Relays.Handshakes
{
    public class ResponseHandshake : RelayResponse
    {
        public ushort ProtocolVersion;
        public ushort ClientId;
        public ClientStatus ClientStatus;
        public IPEndPoint Remote;

        public override bool FromBuffer(Buffer buffer)
        {
            buffer.Goto(0);
            if (buffer.length != 11) return false;
            ProtocolVersion = buffer.ReadUShort();
            ClientId = buffer.ReadUShort();
            ClientStatus = buffer.ReadEnum<ClientStatus>();
            var address = buffer.ReadBytes(4);
            var port = buffer.ReadUShort();
            Remote = new IPEndPoint(new IPAddress(address), port);
            return true;
        }

        public override string ToString() =>
            $"{GetType().Name}[ProtocolVersion={ProtocolVersion}, ClientId={ClientId}, ClientStatus={ClientStatus}, Remote={Remote}]";
    }
}