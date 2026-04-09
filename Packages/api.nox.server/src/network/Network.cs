using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Network;
using Nox.CCK.Utils;

namespace api.nox.server.network
{
	public static class Network
	{
		private static void InvokeFetch(Server server)
		{
			if (server == null) return;
			Main.Instance.CoreAPI.EventAPI.Emit("server_fetch", server);

			var config = Config.Load();
			if (!config.Has(new[] { "servers", server.Address }))
				return;

			// Gateway URL is already cached by NodeDiscover; only persist metadata here
			config.Set(new[] { "servers", server.Address, "title" }, server.Metadata?.Title);
			config.Set(new[] { "servers", server.Address, "features" }, server.Features);

			config.Save();
		}

		public static async UniTask<Server> Fetch(string address, CancellationToken cancellationToken = default)
		{
			if (Main.NetworkAPI == null || string.IsNullOrEmpty(address))
				return null;

			// Reuse the NoxWellKnown already fetched and cached during gateway discovery
			var wk = await NodeDiscover.GetWellKnown(address);
			if (wk == null)
			{
				Logger.LogError($"Failed to fetch server info for {address}: discovery returned no well-known document");
				return null;
			}

			var server = Server.From(wk);
			InvokeFetch(server);
			
			return server;
		}
	}
}