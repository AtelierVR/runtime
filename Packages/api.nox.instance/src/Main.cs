using System.Linq;
using api.nox.instance.network;
using api.nox.instance.search;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.Instances;
using Nox.Network;
using Nox.Search;
using Nox.Sessions;
using Nox.Users;
using Nox.Worlds;
using ISearchRequest = Nox.Instances.ISearchRequest;
using ISearchResponse = Nox.Instances.ISearchResponse;

namespace api.nox.instance {
	public class Main : MainModInitializer, IInstanceAPI {
		internal static Main           Instance;
		internal        MainModCoreAPI CoreAPI;
		internal        Network        Network;
		private         LanguagePack   _language;
		private         Search         _search;

		internal INetworkAPI NetworkAPI
			=> Instance.CoreAPI.ModAPI
				.GetMod("network")
				?.GetMains()
				.FirstOrDefault() as INetworkAPI;

		internal IUserAPI UserAPI
			=> Instance.CoreAPI.ModAPI
				.GetMod("user")
				?.GetMains()
				.FirstOrDefault() as IUserAPI;

		internal IWorldAPI WorldAPI
			=> Instance.CoreAPI.ModAPI
				.GetMod("world")
				?.GetMains()
				.FirstOrDefault() as IWorldAPI;

		internal ISearchAPI SearchAPI
			=> Instance.CoreAPI.ModAPI
				.GetMod("search")
				.GetMains()
				.FirstOrDefault() as ISearchAPI;
		
		internal ISessionAPI SessionAPI
			=> Instance.CoreAPI.ModAPI
				.GetMod("session")
				.GetMains()
				.FirstOrDefault() as ISessionAPI;

		public async UniTask<IInstance> Fetch(IInstanceIdentifier identifier)
			=> await Network.Fetch(InstanceIdentifier.FromBase(identifier));

		public async UniTask<IInstance> Fetch(uint id, string from = null)
			=> await Network.Fetch(id, from);

		public async UniTask<IInstance> Fetch(string identifier, string from = null)
			=> await Network.Fetch(identifier, from);

		public async UniTask<ISearchResponse> Search(ISearchRequest data, string from = null)
			=> await Network.Search(SearchRequest.FromBase(data), from);

		public IInstanceIdentifier Make(string identifier)
			=> InstanceIdentifier.FromString(identifier);


		public ISearchRequest MakeSearchRequest()
			=> new SearchRequest();

		public void OnInitializeMain(MainModCoreAPI api) {
			CoreAPI   = api;
			Instance  = this;
			_language = CoreAPI.AssetAPI.GetAsset<LanguagePack>("lang.asset");
			LanguageManager.AddPack(_language);
			_search = new Search();
			Network = new Network();
		}

		public void OnDisposeMain() {
			LanguageManager.RemovePack(_language);
			_search?.Dispose();
			CoreAPI  = null;
			Instance = null;
		}
	}
}