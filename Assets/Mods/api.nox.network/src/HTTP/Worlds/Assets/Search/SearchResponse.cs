using api.nox.network.Worlds.Assets;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;

namespace api.nox.network.Worlds
{
    [System.Serializable]
    public class SearchResponse : INoxObject
    {
        internal ushort[] versions;
        internal string[] platforms;
        internal string[] engines;
        internal string server;
        internal uint world_id;

        [NoxPublic(NoxAccess.Read)] public WorldAsset[] assets;
        [NoxPublic(NoxAccess.Read)] public uint total;
        [NoxPublic(NoxAccess.Read)] public uint limit;
        [NoxPublic(NoxAccess.Read)] public uint offset;
        [NoxPublic(NoxAccess.Read)] public bool with_empty;

        [NoxPublic(NoxAccess.Method)]
        public bool HasPrevious() => offset > 0;

        [NoxPublic(NoxAccess.Method)]
        public bool HasNext() => offset + limit < total;

        [NoxPublic(NoxAccess.Method)]
        public async UniTask<SearchResponse> Previous()
            => HasPrevious() && NetworkSystem.ModInstance.World != null
                ? await NetworkSystem.ModInstance.World.Asset.SearchAssets(new SearchRequest()
                {
                    Server = server,
                    WorldID = world_id,
                    Offset = offset - limit,
                    Limit = limit,
                    Platforms = platforms,
                    Engines = engines,
                    WithEmpty = with_empty,
                    Versions = versions
                })
                : null;

        [NoxPublic(NoxAccess.Method)]
        public async UniTask<SearchResponse> Next()
            => HasNext() && NetworkSystem.ModInstance.World != null
                ? await NetworkSystem.ModInstance.World.Asset.SearchAssets(new SearchRequest()
                {
                    Server = server,
                    WorldID = world_id,
                    Offset = offset + limit,
                    Limit = limit,
                    Platforms = platforms,
                    Engines = engines,
                    WithEmpty = with_empty,
                    Versions = versions
                })
                : null;

        public override string ToString() => $"{GetType().Name}[world_id={world_id}, server={server}]";
    }
}