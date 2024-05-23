using Nox.Network.Relays.Base;

namespace Nox.Network.Instances.Base
{
    public abstract class InstanceResponse : RelayResponse
    {
        public ushort InternalId;
        public Instance Instance => InstanceManager.Get(InternalId, RelayId);
    }
}