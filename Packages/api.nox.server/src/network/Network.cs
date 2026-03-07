using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Network;
using Nox.CCK.Utils;

namespace api.nox.server.network {
	public static class Network {
		private static void InvokeFetch(Server server) {
			if (server == null) return;
			Main.Instance.CoreAPI.EventAPI.Emit("server_fetch", server);

			var config = Config.Load();
			if (!config.Has(new[] { "servers", server.address }))
				return;

			config.Set(new[] { "servers", server.address, "gateway" }, server.GetGateways().GetHttp());
			config.Set(new[] { "servers", server.address, "title" }, server.title);
			config.Set(new[] { "servers", server.address, "features" }, server.features);
			
			config.Save();
		}

		public static async UniTask<Server> Fetch(string address, CancellationToken cancellationToken = default) {
			if (Main.NetworkAPI == null || string.IsNullOrEmpty(address))
				return null;

			var request = await RequestNode.To(address, "/api/server");
			if (request == null) {
				Logger.LogError($"Failed to create request for server {address}");
				return null;
			}

			await request.Send(cancellationToken);
			var response = await request.Node<Server>(cancellationToken);
			if (response.HasError()) {
				Logger.LogError($"Failed to fetch server {address}: {response.Error.Message}");
				return null;
			}

			var server = response.Data;
			InvokeFetch(server);
			return server;
		}
	}
}