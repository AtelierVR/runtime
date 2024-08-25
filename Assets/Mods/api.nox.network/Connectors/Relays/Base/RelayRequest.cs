using api.nox.network.Utils;

namespace api.nox.network.Relays.Base
{
    public abstract class RelayRequest
    {
        public ushort RelayId;
        public Relay Relay => RelayManager.Get(RelayId);
        public abstract Buffer ToBuffer();
    }
}