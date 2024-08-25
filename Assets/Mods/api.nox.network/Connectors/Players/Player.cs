using System;
using api.nox.network.Instances;
using api.nox.network.Players;
using api.nox.network.Relays;

namespace api.nox.network
{
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    public class Player : Entity
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    {
        public PlayerFlags Flags;
        public ushort Id;
        public string DisplayName;
        public string ServerAddress;
        public DateTime DateReference;
        public ushort RelayId;
        public ushort InternalId;
        public Relay Relay => RelayManager.Get(RelayId);
        public Instances.Instance Instance => InstanceManager.Get(InternalId, RelayId);

        public override string ToString() => $"{GetType().Name}[Id={Id}, DisplayName={DisplayName}]";
        public Utils.Transform GetPart(PlayerRig rig) => Transfroms.TryGetValue((ushort)rig, out var part) ? part : null;

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;
            var player = (Player)obj;
            return Id == player.Id && RelayId == player.RelayId && InternalId == player.InternalId;
        }
    }
}