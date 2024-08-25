using api.nox.network.Relays.Base;

namespace api.nox.network.Instances.Base
{
    public abstract class InstanceRequest : RelayRequest
    {
        public ushort InternalId;
        public Instance Instance => InstanceManager.Get(InternalId, RelayId);
    }
}