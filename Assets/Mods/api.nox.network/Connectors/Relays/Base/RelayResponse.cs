using api.nox.network.Utils;

namespace api.nox.network.Relays.Base
{
    public abstract class RelayResponse
    {
        public ushort UId;
        public ushort RelayId;
        public Relay Relay => RelayManager.Get(RelayId);
        public abstract bool FromBuffer(Buffer buffer);
    }
}