using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;

namespace api.nox.network.Servers
{
    [System.Serializable]
    public class SearchResponse
    {
        [NoxPublic(NoxAccess.Read)] public Server[] servers;
        [NoxPublic(NoxAccess.Read)] public string query;
        [NoxPublic(NoxAccess.Read)] public uint total;
        [NoxPublic(NoxAccess.Read)] public uint limit;
        [NoxPublic(NoxAccess.Read)] public uint offset;
        
        [NoxPublic(NoxAccess.Method)]
        public bool HasNext() => offset + limit < total;

        [NoxPublic(NoxAccess.Method)]
        public bool HasPrevious() => offset > 0;

        [NoxPublic(NoxAccess.Method)]
        public async UniTask<SearchResponse> Next()
            => HasNext() && NetworkSystem.ModInstance.Server != null
                ? await NetworkSystem.ModInstance.Server.SearchServers(new SearchRequest
                {
                    Query = query,
                    Offset = offset + limit,
                    Limit = limit
                })
                : null;

        [NoxPublic(NoxAccess.Method)]
        public async UniTask<SearchResponse> Previous()
            => HasPrevious() && NetworkSystem.ModInstance.Server != null
                ? await NetworkSystem.ModInstance.Server.SearchServers(new SearchRequest
                {
                    Query = query,
                    Offset = offset - limit,
                    Limit = limit
                })
                : null;
    }
}