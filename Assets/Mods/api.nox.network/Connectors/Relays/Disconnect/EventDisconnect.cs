using api.nox.network.Relays.Base;
using api.nox.network.Utils;

namespace api.nox.network.Relays.Disconnect
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