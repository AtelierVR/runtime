using System.Linq;
using api.nox.user.network;
using api.nox.user.search;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using Nox.Network;
using Nox.Search;
using Nox.Servers;
using Nox.Users;
using Nox.Worlds;

namespace api.nox.user {
	public class Main : MainModInitializer, IUserAPI {
		internal static Main           Instance;
		internal        MainModCoreAPI CoreAPI;
		private         LanguagePack   _language;
		internal        Network        Network;
		private         Search         _search;

		public IServerAPI ServerAPI
			=> Instance.CoreAPI.ModAPI
				.GetMod("server")
				?.GetMains()
				.FirstOrDefault() as IServerAPI;

		public INetworkAPI NetworkAPI
			=> Instance.CoreAPI.ModAPI
				.GetMod("network")
				?.GetMains()
				.FirstOrDefault() as INetworkAPI;

		public IWorldAPI WorldAPI
			=> Instance.CoreAPI.ModAPI
				.GetMod("world")
				?.GetMains()
				.FirstOrDefault() as IWorldAPI;

		internal ISearchAPI SearchAPI
			=> Main.Instance.CoreAPI.ModAPI
				.GetMod("search")
				.GetMains()
				.FirstOrDefault() as ISearchAPI;

		public async UniTask OnInitializeMainAsync(MainModCoreAPI api) {
			CoreAPI   = api;
			Instance  = this;
			Network   = new Network();
			_language = api.AssetAPI.GetAsset<LanguagePack>("lang.asset");
			LanguageManager.AddPack(_language);

			_search = new Search();

			var user = Network.CurrentUser;
			user ??= await Network.FetchCurrent();

			if (user == null)
				Logger.LogDebug("User not found");
			else Logger.LogDebug("User found: " + user.GetUsername());
		}

		public void OnPostInitializeMain() { }

		public void OnDisposeMain() {
			_search.Dispose();
			_search = null;
			Network.Dispose();
			Network = null;
			LanguageManager.RemovePack(_language);
			_language = null;
			CoreAPI   = null;
			Instance  = null;
		}

		public ICurrentUser GetCurrent()
			=> Network.CurrentUser;

		public async UniTask<ICurrentUser> FetchCurrent()
			=> await Network.FetchCurrent();

		public async UniTask<IUser> Fetch(IUserIdentifier identifier)
			=> await Network.Fetch(UserIdentifier.FromBase(identifier));

		public async UniTask<IUser> Fetch(uint id, string from = null)
			=> await Network.Fetch(id, from);

		public async UniTask<IUser> Fetch(string identifier, string from = null)
			=> await Network.Fetch(identifier, from);

		public IUserIdentifier Make(string identifier)
			=> UserIdentifier.FromString(identifier);

		public async UniTask<Nox.Users.ISearchResponse> Search(Nox.Users.ISearchRequest request)
			=> await Network.Search(SearchRequest.FromBase(request));

		public async UniTask<IAuthToken> GetToken(string address)
			=> await Network.GetToken(address);

		public async UniTask<IntegrityResponse> CreateIntegrity(string address)
			=> await Network.CreateIntegrity(address);
	}
}