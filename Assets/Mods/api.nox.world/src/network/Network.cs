using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using UnityEngine.Events;

namespace api.nox.world.network {
	public class Network {
		private readonly UnityEvent<World> _fetchEvent = new();

		private void InvokeFetch(World world) {
			if (world == null) return;
			_fetchEvent.Invoke(world);
			Main.Instance.CoreAPI.EventAPI.Emit("world_fetch", world);
		}

		public UniTask<World> Fetch(WorldIdentifier identifier, string from = null)
			=> Fetch(identifier.ToString(), from);

		public UniTask<World> Fetch(uint id, string from = null)
			=> Fetch(id.ToString(), from);

		public async UniTask<World> Fetch(string identifier, string from = null) {
			if (Main.Instance.NetworkAPI == null)
				return null;
			var ide = WorldIdentifier.FromString(identifier);
			if (ide.IsLocal())
				ide.Server = from;
			var address = from ?? Main.Instance.UserAPI?.GetCurrent()?.GetServerAddress() ?? ide.GetServerAddress();
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError($"Cannot fetch user {identifier}: no server address provided.");
				return null;
			}

			var request = Main.Instance.NetworkAPI.MakeRequest();
			request.SetMasterUrl(address, $"/api/worlds/{ide.ToString()}");
			await request.Send();
			var response = request.GetMasterResponse<World>();
			if (response.HasError()) {
				Logger.LogError($"Failed to fetch user {identifier} from {address}: {response.GetError().GetMessage()}");
				return null;
			}

			var world = response.GetData();
			InvokeFetch(world);
			return world;
		}


		public async UniTask<SearchResponse> Search(SearchRequest data, string from = null) {
			if (Main.Instance.NetworkAPI == null)
				return null;
			var address = from ?? Main.Instance.UserAPI?.GetCurrent()?.GetServerAddress();
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError("Cannot search users: no server address provided.");
				return null;
			}

			var request = Main.Instance.NetworkAPI.MakeRequest();
			request.SetMasterUrl(address, $"/api/worlds?{data.ToParams()}");
			await request.Send();
			var response = request.GetMasterResponse<SearchResponse>();
			Logger.LogDebug(request.GetResponse<string>());
			if (response.HasError()) {
				Logger.LogError($"Failed to search users from {address}: {response.GetError().GetMessage()}");
				return null;
			}

			var users = response.GetData();

			foreach (var user in users.worlds)
				InvokeFetch(user);

			return users;
		}
	}
}