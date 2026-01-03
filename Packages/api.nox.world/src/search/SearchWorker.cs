using api.nox.world.network;
using Cysharp.Threading.Tasks;
using Nox.CCK.Worlds;
using Nox.Search;

namespace api.nox.world.search {
	public class SearchWorker : IWorker {
		public string Title;
		public string ServerAddress;

		public string GetTitleKey()
			=> "user.search.worker.title";

		public string[] GetTitleArguments()
			=> new[] { Title };

		public float GetRatio()
			=> 4f / 3f;

		public async UniTask<IResult> Fetch(IFetchOptions options) {
			if (string.IsNullOrEmpty(ServerAddress))
				return new SearchResult { Error = "Invalid server address." };
			var data = await Main.Instance.Network.Search(
				new SearchRequest {
					Query  = options.GetQuery(),
					Offset = options.GetPage() * options.GetLimit(),
					Limit  = options.GetLimit(),
				}, ServerAddress
			);
			if (data == null) return new SearchResult { Error = "Error fetching users." };
			return new SearchResult {
				Response      = data,
				ServerAddress = ServerAddress,
				Error         = null
			};
		}
	}
}