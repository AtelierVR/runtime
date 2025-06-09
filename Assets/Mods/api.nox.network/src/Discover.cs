using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;

namespace api.nox.network {
	public class Discover {
		public static async UniTask<string> GetGateway(string server) {
			var config  = Config.Load();
			var gateway = config.Get<string>(new[] { "servers", server, "gateway" });
			if (gateway != null) return gateway;
			var req = await Gateway.FindGatewayMaster(server);
			if (req == null) return null;
			config.Set(new[] { "servers", server, "gateway" }, req.OriginalString);
			config.Save();
			return req.OriginalString;
		}
	}
}