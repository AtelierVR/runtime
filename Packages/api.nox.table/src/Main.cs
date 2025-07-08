using System.Linq;
using api.nox.table.network;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using Nox.Network;
using Nox.Tables;
using Nox.Users;

namespace api.nox.table {
	public class Main : MainModInitializer, ITableAPI {
		#region Variables

		internal static Main       Instance;
		internal        ModCoreAPI CoreAPI;

		internal static INetworkAPI NetworkAPI
			=> Instance.CoreAPI.ModAPI
				.GetMod("network")
				?.GetMains()
				.FirstOrDefault() as INetworkAPI;

		internal static IUserAPI UserAPI
			=> Instance.CoreAPI.ModAPI
				.GetMod("user")
				?.GetMains()
				.FirstOrDefault() as IUserAPI;

		internal Network Network;

		#endregion

		#region ModInitializer

		public void OnInitialize(ModCoreAPI api) {
			CoreAPI  = api;
			Instance = this;
			Network  = new Network();
		}

		public void OnDispose() {
			CoreAPI  = null;
			Instance = null;
		}

		#endregion

		[NoxPublic(NoxAccess.Method)]
		public async UniTask<IEntry> Get(string key, string from = null)
			=> await Network.Get(key, from);

		[NoxPublic(NoxAccess.Method)]
		public async UniTask<IEntry> Set(string key, string value, string from = null)
			=> await Network.Set(key, value, from);

		[NoxPublic(NoxAccess.Method)]
		public async UniTask<IEntry> Delete(string key, string from = null)
			=> await Network.Delete(key, from);
	}
}