using System.IO;
using System.Threading;
using api.nox.user.network;
using api.nox.user.search;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Network;
using Nox.CCK.Users;
using Nox.CCK.Utils;
using Nox.Network;
using Nox.Search;
using Nox.Servers;
using Nox.Users;
using UnityEngine.Networking;

namespace api.nox.user {
	public class Main : IMainModInitializer, IUserAPI {
		internal static Main Instance;
		internal IMainModCoreAPI CoreAPI;
		internal Network Network;
		private LanguagePack _language;
		private Search _search;

		public static IServerAPI ServerAPI
			=> Instance.CoreAPI.ModAPI
				.GetMod("server")
				?.GetInstance<IServerAPI>();

		public static INetworkAPI NetworkAPI
			=> Instance.CoreAPI.ModAPI
				.GetMod("network")
				?.GetInstance<INetworkAPI>();

		internal static ISearchAPI SearchAPI
			=> Main.Instance.CoreAPI.ModAPI
				.GetMod("search")
				?.GetInstance<ISearchAPI>();

		public async UniTask OnInitializeMainAsync(IMainModCoreAPI api) {
			CoreAPI = api;
			Instance = this;
			RequestNode.OnCreated.AddListener(OnBeforeRequest);
			Network = new Network();
			_language = api.AssetAPI.GetAsset<LanguagePack>("lang.asset");
			LanguageManager.AddPack(_language);

			_search = new Search();

			var user = Network.CurrentUser;
			user ??= await Network.FetchCurrent();

			if (user == null)
				Logger.LogDebug("User not found");
			else Logger.LogDebug("User found: " + user.GetUsername());
		}

		private async UniTask OnBeforeRequest(string address, UnityWebRequest request) {
			var token = await GetToken(address);
			if (token != null)
				request.SetRequestHeader("Authorization", token.ToHeader());

			var uid = GetCurrent()?.ToIdentifier()?.ToString();
			if (!string.IsNullOrEmpty(uid))
				request.SetRequestHeader("X-Nox-User", uid);
		}

		public void OnPostInitializeMain() { }

		public void OnDisposeMain() {
			RequestNode.OnCreated.RemoveListener(OnBeforeRequest);
			_search.Dispose();
			Network.Dispose();
			LanguageManager.RemovePack(_language);
			_search = null;
			Network = null;
			_language = null;
			CoreAPI = null;
			Instance = null;
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
			=> UserIdentifier.From(identifier);

		public IUserIdentifier Make(uint id, string server = "::")
			=> new UserIdentifier(id, server);

		public ISearchRequest MakeSearchRequest()
			=> new SearchRequest();

		public async UniTask<ISearchResponse> Search(ISearchRequest request, string from = null)
			=> await Network.Search(SearchRequest.FromBase(request), from);

		public async UniTask<IAuthToken> GetToken(string address)
			=> await Network.GetToken(address);

		public async UniTask<IntegrityResponse> CreateIntegrity(string address)
			=> await Network.CreateIntegrity(address);

		public async UniTask<ICurrentUser> UpdateCurrent(IUpdateCurrentUserRequest request)
			=> await Network.UpdateCurrentUser(UpdateCurrentUserRequest.FromBase(request));

		public IUpdateCurrentUserRequest MakeUpdateCurrentRequest()
			=> new UpdateCurrentUserRequest();
	}
}