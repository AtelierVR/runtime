using System;
using Cysharp.Threading.Tasks;

namespace api.nox.network.Instances
{
    [Serializable]
    public class SearchResponse
    {
        public Instance[] instances;
        internal string query;
        internal string world;
        internal string owner;
        public uint total;
        public uint limit;
        public uint offset;

        public bool HasNext() => offset + limit < total;
        public bool HasPrevious() => offset > 0;

        public async UniTask<SearchResponse> Next()
            => HasNext() && NetworkSystem.ModInstance.Instance != null  ? await NetworkSystem.ModInstance.Instance.SearchInstances(new SearchRequest()
            {
                query = query,
                world = world,
                owner = owner,
                offset = offset + limit,
                limit = limit
            }) : null;

        public async UniTask<SearchResponse> Previous()
            => HasPrevious() && NetworkSystem.ModInstance.Instance != null ? await NetworkSystem.ModInstance.Instance.SearchInstances(new SearchRequest()
            {
                query = query,
                world = world,
                owner = owner,
                offset = offset - limit,
                limit = limit
            }) : null;
    }
}