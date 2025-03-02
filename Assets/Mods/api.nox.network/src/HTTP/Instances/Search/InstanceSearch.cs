using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;

namespace api.nox.network.Instances
{
    [Serializable]
    public class SearchResponse : INoxObject
    {
        internal string query;
        internal string world;
        internal string owner;
        
        [NoxPublic(NoxAccess.Read)] public Instance[] instances;
        [NoxPublic(NoxAccess.Read)] public uint total;
        [NoxPublic(NoxAccess.Read)] public uint limit;
        [NoxPublic(NoxAccess.Read)] public uint offset;

        [NoxPublic(NoxAccess.Method)]
        public bool HasNext() => offset + limit < total;

        [NoxPublic(NoxAccess.Method)]
        public bool HasPrevious() => offset > 0;

        [NoxPublic(NoxAccess.Method)]
        public async UniTask<INoxObject> Next()
            => await NextInternal();

        [NoxPublic(NoxAccess.Method)]
        public async UniTask<INoxObject> Previous()
            => await PreviousInternal();


        public async UniTask<SearchResponse> NextInternal()
            => HasNext() && NetworkSystem.ModInstance.Instance != null
                ? await InstanceAPI.SearchInstances(new SearchRequest()
                {
                    Query = query,
                    World = world,
                    Owner = owner,
                    Offset = offset + limit,
                    Limit = limit
                })
                : null;

        public async UniTask<SearchResponse> PreviousInternal()
            => HasPrevious() && NetworkSystem.ModInstance.Instance != null
                ? await InstanceAPI.SearchInstances(new SearchRequest()
                {
                    Query = query,
                    World = world,
                    Owner = owner,
                    Offset = offset - limit,
                    Limit = limit
                })
                : null;
    }
}