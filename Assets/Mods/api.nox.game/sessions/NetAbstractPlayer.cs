using api.nox.network;
using api.nox.network.Players;
using api.nox.network.Utils;

namespace api.nox.game.sessions
{
    public class NetAbstractPlayer : IAbstractPlayer
    {
        internal NetPlayer player;

        internal Session _session;

        public Session GetSession() => _session;
        internal void SetSession(Session session) => _session = session;
        void IAbstractPlayer.SetSession(Session session) => SetSession(session);

        internal NetAbstractPlayer(NetPlayer player, Session session)
        {
            this.player = player;
            _session = session;
        }

        public ushort GetId() => player.Id;

        public bool IsLocal() => player is NetLocalPlayer;

        public bool IsRemote() => player is NetPlayer;

        public bool TryGetPhysical(out PhysicalPlayer player)
        {
            player = null;
            return false;
        }

        public void Unregister() => GetSession()?.UnregisterPlayer(this);
        public void Register() => GetSession()?.RegisterPlayer(this);

        public void Teleport(UnityEngine.Transform transform)
            => player.SetPart(PlayerRig.Base, new Transform(transform) { deleveryType = TransformDeleveryType.LocalModified });

        public Transform GetTransform(PlayerRig rig)
            => player.GetPart(rig);
        
        public PlayerPart[] GetParts() => player.GetParts();

        public void SetTransform(PlayerRig rig, Transform transform)
            => player.SetPart(rig, transform);
    }
}