using Nox.Network.Relays.Base;
using Nox.Scripts;

namespace Nox.Network.Relays.Disconnect
{
    public class EventDisconnect : RelayResponse
    {
        public string Reason;

        public override bool FromBuffer(Buffer buffer)
        {
            if (buffer.length > 0)
                Reason = buffer.ReadString();
            return true;
        }
    }
}