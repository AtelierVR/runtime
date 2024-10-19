using System;
using api.nox.network.Worlds;
using Cysharp.Threading.Tasks;

namespace api.nox.network.Users
{
    [Serializable]
    public class UserMe : User
    {
        public string email;
        public uint createdAt;
        public string home;

        public override bool MatchRef(string reference, string default_server)
        {
            var identifier = UserIdentifier.FromString(reference);
            if (new UserIdentifier(id.ToString(), server).ToMinimalString() == identifier.ToMinimalString(default_server)) return true;
            if (new UserIdentifier(username, server).ToMinimalString() == identifier.ToMinimalString(default_server)) return true;
            return false;
        }

        public async UniTask<World> GetHome()
        {
            if (string.IsNullOrEmpty(home)) return null;
            var worldref = WorldIdentifier.FromString(home);
            return await NetworkSystem.ModInstance.World.GetWorld(worldref.server ?? server, worldref.id);
        } 

    }
}