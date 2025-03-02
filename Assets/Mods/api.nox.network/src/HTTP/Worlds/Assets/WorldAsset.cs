using System;
using Nox.CCK.Utils;

namespace api.nox.network.Worlds.Assets
{
    [Serializable]
    public class WorldAsset : ICached, INoxObject
    {
        [NoxPublic(NoxAccess.Read)] public uint id;
        [NoxPublic(NoxAccess.Read)] public ushort version;
        [NoxPublic(NoxAccess.Read)] public string engine;
        [NoxPublic(NoxAccess.Read)] public string platform;
        [NoxPublic(NoxAccess.Read)] public bool is_empty;
        [NoxPublic(NoxAccess.Read)] public string url;
        [NoxPublic(NoxAccess.Read)] public string hash;
        [NoxPublic(NoxAccess.Read)] public uint size;
        [NoxPublic(NoxAccess.Read)] public string server;
        [NoxPublic(NoxAccess.Read)] public uint world_id;

        [NoxPublic(NoxAccess.Method)]
        public bool IsEmpty() => is_empty || string.IsNullOrEmpty(url) || string.IsNullOrEmpty(hash) || size == 0;

        [NoxPublic(NoxAccess.Method)]
        public Platform GetPlatform() => PlatformExtensions.GetPlatformFromName(platform);

        [NoxPublic(NoxAccess.Method)]
        public Engine GetEngine() => EngineExtensions.GetEngineFromName(engine);

        public string GetCacheKey() => GetCacheKey(id, world_id, server);

        public static string GetCacheKey(uint id, uint worldId, string server)
            => $"world_asset.{id}.{worldId}.{server}";

        public override string ToString() => $"{GetType().Name}[id={id}, world_id={world_id}, server={server}]";
    }
}