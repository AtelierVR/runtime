using Cysharp.Threading.Tasks;

namespace api.nox.network.Worlds
{
    [System.Serializable]
    public class SearchResponse
    {
        internal ushort[] versions;
        internal string[] platforms;
        internal string[] engines;
        internal string server;
        internal uint world_id;
        public Assets.WorldAsset[] assets;

        public uint total;
        public uint limit;
        public uint offset;
        public bool with_empty;

        public bool HasPrevious() => offset > 0;
        public bool HasNext() => offset + limit < total;

        public async UniTask<SearchResponse> Previous()
            => HasPrevious() && NetworkSystem.ModInstance.World != null ? await NetworkSystem.ModInstance.World.Asset.SearchAssets(new()
            {
                server = server,
                world_id = world_id,
                offset = offset - limit,
                limit = limit,
                platforms = platforms,
                engines = engines,
                with_empty = with_empty,
                versions = versions
            }) : null;

        public async UniTask<SearchResponse> Next()
            => HasNext() && NetworkSystem.ModInstance.World != null ? await NetworkSystem.ModInstance.World.Asset.SearchAssets(new()
            {
                server = server,
                world_id = world_id,
                offset = offset + limit,
                limit = limit,
                platforms = platforms,
                engines = engines,
                with_empty = with_empty,
                versions = versions
            }) : null;
    }
}