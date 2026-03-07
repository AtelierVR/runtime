using System.Linq;
using api.nox.instance.network;
using api.nox.instance.search;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
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
	public class Main : IMainModInitializer, IInstanceAPI {
		internal static Main           Instance;
		internal        IMainModCoreAPI CoreAPI;
		internal        Network        Network;
		private         LanguagePack   _language;
		private         Search         _search;

		internal static INetworkAPI NetworkAPI
			=> Instance.CoreAPI.ModAPI
				.GetMod("network")
				?.GetInstance<INetworkAPI>();

		internal static IUserAPI UserAPI
			=> Instance.CoreAPI.ModAPI
				.GetMod("users")
				?.GetInstance<IUserAPI>();

		internal static IWorldAPI WorldAPI
			=> Instance.CoreAPI.ModAPI
				.GetMod("world")
				?.GetInstance<IWorldAPI>();

		static internal ISearchAPI SearchAPI
			=> Instance.CoreAPI.ModAPI
				.GetMod("search")
				?.GetInstance<ISearchAPI>();

		static internal ISessionAPI SessionAPI
			=> Instance.CoreAPI.ModAPI
				.GetMod("session")
				?.GetInstance<ISessionAPI>();

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

		public IInstanceIdentifier Make(uint id, string from)
			=> new InstanceIdentifier(id, null, from);
		
		public ISearchRequest MakeSearchRequest()
			=> new SearchRequest();

		public void OnInitializeMain(IMainModCoreAPI api) {
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