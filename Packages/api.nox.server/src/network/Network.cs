using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;

namespace api.nox.server.network {
	public class Network {
		private void InvokeFetch(Server server) {
			if (server == null) return;
			Main.Instance.CoreAPI.EventAPI.Emit("server_fetch", server);
		}

		public async UniTask<Server> Fetch(string address) {
			if (Main.NetworkAPI == null || string.IsNullOrEmpty(address))
				return null;

			var request = Main.NetworkAPI.MakeRequest();
			await request.SetMasterUrl(address, "/api/server");
			await request.Send();
			var response = request.GetMasterResponse<Server>();
			if (response.HasError()) {
				Logger.LogError($"Failed to fetch server {address}: {response.GetError().GetMessage()}");
				return null;
			}

			var server = response.GetData();
			InvokeFetch(server);
			return server;
		}
	}
}