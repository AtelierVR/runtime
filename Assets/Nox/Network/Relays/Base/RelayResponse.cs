using Nox.Scripts;

namespace Nox.Network.Relays.Base
{
    public abstract class RelayResponse
    {
        public ushort UId;
        public ushort RelayId;
        public Relay Relay => RelayManager.Get(RelayId);
        public abstract bool FromBuffer(Buffer buffer);
    }
}