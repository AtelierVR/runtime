using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;

namespace api.nox.network.Worlds
{
    [Serializable]
    public class World : ICached, INoxObject
    {
        [NoxPublic(NoxAccess.Read)] public uint id;
        [NoxPublic(NoxAccess.Read)] public string title;
        [NoxPublic(NoxAccess.Read)] public string description;
        [NoxPublic(NoxAccess.Read)] public ushort capacity;
        [NoxPublic(NoxAccess.Read)] public string[] tags;
        [NoxPublic(NoxAccess.Read)] public string owner;
        [NoxPublic(NoxAccess.Read)] public string server;
        [NoxPublic(NoxAccess.Read)] public string thumbnail;

        public string GetCacheKey() => GetCacheKey(id, server);
        public static string GetCacheKey(uint id, string server) => $"world.{id}.{server}";

        [NoxPublic(NoxAccess.Method)]
        public virtual bool MatchRef(string reference, string defaultServer)
            => ToIdentifier().ToMinimalString()
               == UserIdentifier.FromString(reference)
                   .ToMinimalString(defaultServer);
        
        [NoxPublic(NoxAccess.Method)]
        public bool IsHome()
        {
            var user = NetworkSystem.ModInstance.User.CurrentUser;
            return user?.home != null && MatchRef(user.home, server);
        }

        [NoxPublic(NoxAccess.Method)]
        public WorldIdentifier ToIdentifier() => new(id, server);
        
        
        [NoxPublic(NoxAccess.Method)]
        public virtual async UniTask<bool> Refresh()
        {
            var world = await NetworkSystem.ModInstance.World.GetWorldById(server, id);
            if (world == null) return false;
            world.CopyTo(this);
            NetworkSystem.CoreAPI.EventAPI.Emit(new NetEventContext("world_fetch", this));
            NetCache.Set(this);
            return true;
        }


        internal void CopyTo(World world)
        {
            id = world.id;
            title = world.title;
            description = world.description;
            capacity = world.capacity;
            tags = world.tags;
            owner = world.owner;
            server = world.server;
            thumbnail = world.thumbnail;
        }

        public override string ToString() => $"{GetType().Name}[id={id}, server={server}]";
    }
}