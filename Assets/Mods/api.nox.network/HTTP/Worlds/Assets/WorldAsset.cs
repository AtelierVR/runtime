using System;
using api.nox.network.HTTP;

namespace api.nox.network.Worlds.Assets
{
    [Serializable]
    public class WorldAsset : ICached
    {
        public uint id;
        public ushort version;
        public string engine;
        public string platform;
        public bool is_empty;
        public string url;
        public string hash;
        public uint size;
        public string server;
        public uint world_id;
        public bool IsEmpty() => is_empty || string.IsNullOrEmpty(url) || string.IsNullOrEmpty(hash) || size == 0;
        public string GetCacheKey() => $"world_asset.{id}.{world_id}.{server}";
    }
}