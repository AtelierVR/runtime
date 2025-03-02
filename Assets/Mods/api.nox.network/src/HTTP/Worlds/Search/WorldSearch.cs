using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;

namespace api.nox.network.Worlds
{
    [System.Serializable]
    public class WorldSearch : INoxObject
    {
        internal string Query;
        internal uint[] WorldIds;
        internal string Server;

        [NoxPublic(NoxAccess.Read)] public World[] worlds;
        [NoxPublic(NoxAccess.Read)] public uint total;
        [NoxPublic(NoxAccess.Read)] public uint limit;
        [NoxPublic(NoxAccess.Read)] public uint offset;

        [NoxPublic(NoxAccess.Method)]
        public bool HasNext() => offset + limit < total;

        [NoxPublic(NoxAccess.Method)]
        public bool HasPrevious() => offset > 0;

        [NoxPublic(NoxAccess.Method)]
        public async UniTask<WorldSearch> Next()
            => HasNext() && NetworkSystem.ModInstance.World != null
                ? await NetworkSystem.ModInstance.World.SearchWorlds(new SearchWorldData()
                {
                    Server = Server,
                    Query = Query,
                    WorldIds = WorldIds,
                    Offset = offset + limit,
                    Limit = limit
                })
                : null;

        [NoxPublic(NoxAccess.Method)]
        public async UniTask<WorldSearch> Previous()
            => HasPrevious() && NetworkSystem.ModInstance.World != null
                ? await NetworkSystem.ModInstance.World.SearchWorlds(new SearchWorldData()
                {
                    Server = Server,
                    Query = Query,
                    WorldIds = WorldIds,
                    Offset = offset - limit,
                    Limit = limit
                })
                : null;

        public override string ToString() =>
            $"{GetType().Name}[total={total}, limit={limit}, offset={offset}, worlds={worlds?.Length}]";
    }
}