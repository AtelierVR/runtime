using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using Nox.CCK.Worlds;
using Nox.Worlds;

namespace api.nox.world.network {
	[Serializable]
	public class SearchResponse : ISearchResponse, INoxObject {
		internal string  query;
		internal uint[]  ids;
		public   World[] worlds;
		public   uint    total;
		public   uint    limit;
		public   uint    offset;

		[NoxPublic(NoxAccess.Method)]
		public string GetQuery()
			=> query;

		[NoxPublic(NoxAccess.Method)]
		public uint[] GetIds()
			=> ids;

		[NoxPublic(NoxAccess.Method)]
		public IWorld[] GetWorlds()
			=> worlds.Cast<IWorld>()
				.ToArray();

		[NoxPublic(NoxAccess.Method)]
		public uint GetTotal()
			=> total;

		[NoxPublic(NoxAccess.Method)]
		public uint GetLimit()
			=> limit;

		[NoxPublic(NoxAccess.Method)]
		public uint GetOffset()
			=> offset;

		[NoxPublic(NoxAccess.Method)]
		public bool HasNext()
			=> offset + limit < total;

		[NoxPublic(NoxAccess.Method)]
		public bool HasPrevious()
			=> offset > 0;

		[NoxPublic(NoxAccess.Method)]
		public async UniTask<ISearchResponse> Next()
			=> await Internal_Next();

		[NoxPublic(NoxAccess.Method)]
		public async UniTask<ISearchResponse> Previous()
			=> await Internal_Previous();

		public async UniTask<SearchResponse> Internal_Next()
			=> HasNext()
				? await Main.Instance.Network.Search(
					new SearchRequest {
						Query  = query,
						Ids    = ids,
						Offset = offset + limit,
						Limit  = limit
					}
				)
				: null;

		[NoxPublic(NoxAccess.Method)]
		public async UniTask<SearchResponse> Internal_Previous()
			=> HasPrevious()
				? await Main.Instance.Network.Search(
					new SearchRequest {
						Query  = query,
						Ids    = ids,
						Offset = offset - limit,
						Limit  = limit
					}
				)
				: null;
	}
}