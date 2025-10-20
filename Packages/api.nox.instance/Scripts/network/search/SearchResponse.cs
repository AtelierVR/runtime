using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using Nox.Instances;
using ISearchResponse = Nox.Instances.ISearchResponse;

namespace api.nox.instance.network {
	[Serializable]
	public class SearchResponse : ISearchResponse, INoxObject {
		internal string     query;
		internal string     world;
		internal string     owner;
		public   Instance[] instances;
		public   uint       total;
		public   uint       limit;
		public   uint       offset;

		[NoxPublic(NoxAccess.Method)]
		public string GetQuery()
			=> query;

		[NoxPublic(NoxAccess.Method)]
		public string GetOwnerId()
			=> owner;

		[NoxPublic(NoxAccess.Method)]
		public string GetWorldId()
			=> world;

		[NoxPublic(NoxAccess.Method)]
		public IInstance[] GetInstances()
			=> instances.Cast<IInstance>()
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
						Query = query,
						World = !string.IsNullOrEmpty(world)
							? Main.WorldAPI.Make(world)
							: null,
						Owner = !string.IsNullOrEmpty(owner)
							? Main.UserAPI.Make(owner)
							: null,
						Limit = limit
					}
				)
				: null;

		[NoxPublic(NoxAccess.Method)]
		public async UniTask<SearchResponse> Internal_Previous()
			=> HasPrevious()
				? await Main.Instance.Network.Search(
					new SearchRequest {
						Query = query,
						World = !string.IsNullOrEmpty(world)
							? Main.WorldAPI.Make(world)
							: null,
						Owner = !string.IsNullOrEmpty(owner)
							? Main.UserAPI.Make(owner)
							: null,
						Offset = offset - limit,
						Limit  = limit
					}
				)
				: null;
	}
}