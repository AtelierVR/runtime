using Nox.Scripts;

namespace Nox.Network.Relays.Base
{
    public abstract class RelayRequest
    {
        public ushort RelayId;
        public Relay Relay => RelayManager.Get(RelayId);
        public abstract Buffer ToBuffer();
        
        
    }
}