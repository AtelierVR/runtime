using api.nox.network.Relays.Base;

namespace api.nox.network.RelayInstances.Base
{
    public abstract class InstanceRequest : RelayRequest
    {
        public ushort InternalId;
        public RelayInstance Instance => RelayInstanceManager.Get(InternalId, RelayId);
    }
}