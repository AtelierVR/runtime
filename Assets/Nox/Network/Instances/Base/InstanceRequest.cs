using Nox.Network.Relays.Base;
using Nox.Scripts;

namespace Nox.Network.Instances.Base
{
    public abstract class InstanceRequest : RelayRequest
    {
        public ushort InternalId;
        public Instance Instance => InstanceManager.Get(InternalId, RelayId);
    }
}