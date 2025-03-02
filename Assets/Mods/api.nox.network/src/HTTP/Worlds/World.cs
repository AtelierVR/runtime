using System;
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
        public WorldIdentifier ToIdentifier() => new(id, server);
        public override string ToString() => $"{GetType().Name}[id={id}, server={server}]";
    }
}