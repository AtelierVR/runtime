using System;
using api.nox.network.RelayInstances;
using api.nox.network.Players;
using api.nox.network.Relays;
using api.nox.network.Utils;
using System.Collections.Generic;
using UnityEngine;
using Transform = api.nox.network.Utils.Transform;

namespace api.nox.network
{
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    public class NetPlayer : Entity
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
        public RelayInstance Instance => RelayInstanceManager.Get(InternalId, RelayId);

        public override string ToString() => $"{GetType().Name}[Id={Id}, DisplayName={DisplayName}]";
        public Transform GetPart(PlayerRig rig) => Transfroms.TryGetValue((ushort)rig, out var part) ? part : null;
        public void SetPart(PlayerRig rig, Transform transform)
        {
            var trs = Transfroms;
            trs[(ushort)rig] = transform;
            Transfroms = trs;
        }

        public PlayerPart[] GetParts()
        {
            List<PlayerPart> parts = new();
            foreach (var part in Transfroms)
                parts.Add(new PlayerPart { Rig = (PlayerRig)part.Key, Transform = part.Value });
            return parts.ToArray();
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;
            var player = (NetPlayer)obj;
            return Id == player.Id && RelayId == player.RelayId && InternalId == player.InternalId;
        }
    }
}