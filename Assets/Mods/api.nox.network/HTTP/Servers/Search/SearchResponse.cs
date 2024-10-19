using Cysharp.Threading.Tasks;

namespace api.nox.network.Servers
{
    [System.Serializable]
    public class SearchResponse
    {
        public Server[] servers;
        public string query;
        public uint total;
        public uint limit;
        public uint offset;

        public bool HasNext() => offset + limit < total;
        public bool HasPrevious() => offset > 0;

        public async UniTask<SearchResponse> Next()
            => HasNext() && NetworkSystem.ModInstance.Server != null ? await NetworkSystem.ModInstance.Server.SearchServers(new() 
            {
                query = query,
                offset = offset + limit,
                limit = limit
            }) : null;
        
        public async UniTask<SearchResponse> Previous()
            => HasPrevious() && NetworkSystem.ModInstance.Server != null ? await NetworkSystem.ModInstance.Server.SearchServers(new()
            {
                query = query,
                offset = offset - limit,
                limit = limit
            }) : null;
    }
}