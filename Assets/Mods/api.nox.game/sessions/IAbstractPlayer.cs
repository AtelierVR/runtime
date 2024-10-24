using api.nox.network.Players;
using api.nox.network.Utils;

namespace api.nox.game.sessions
{
    public interface IAbstractPlayer
    {
        ushort GetId();

        public Session GetSession();
        internal void SetSession(Session session);

        bool IsLocal();
        bool IsRemote();
        bool TryGetPhysical(out PhysicalPlayer player);

        void Teleport(UnityEngine.Transform transform);

        Transform GetTransform(PlayerRig rig);
        PlayerPart[] GetParts();
        void SetTransform(PlayerRig rig, Transform transform);


        void TickUpdate() { }
        void FixUpdate() { }
        void LateUpdate() { }
        void Update() { }

        void Unregister();
        void Register();
    }
}