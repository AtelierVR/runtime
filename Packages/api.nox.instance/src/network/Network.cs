using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using UnityEngine.Events;

namespace api.nox.instance.network {
	public class Network {
		private readonly UnityEvent<Instance> _fetchEvent = new();

		private void InvokeFetch(Instance instance) {
			if (instance == null) return;
			_fetchEvent.Invoke(instance);
			Main.Instance.CoreAPI.EventAPI.Emit("instance_fetch", instance);
		}

		public UniTask<Instance> Fetch(InstanceIdentifier identifier, string from = null)
			=> Fetch(identifier.ToString(), from);

		public UniTask<Instance> Fetch(uint id, string from = null)
			=> Fetch(id.ToString(), from);

		public async UniTask<Instance> Fetch(string identifier, string from = null) {
			if (Main.Instance.NetworkAPI == null)
				return null;

			var ide = InstanceIdentifier.FromString(identifier);
			if (ide.IsLocal())
				ide.Server = from;
			var address = from ?? Main.Instance.UserAPI?.GetCurrent()?.GetServerAddress() ?? ide.GetServerAddress();
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError($"Cannot fetch user for {identifier}: no server address provided.");
				return null;
			}

			if (address == ide.GetServerAddress())
				ide.Server = "::"; // Use "::" to indicate local server in the identifier

			var request = Main.Instance.NetworkAPI.MakeRequest();
			await request.SetMasterUrl(address, $"/api/instance/{ide.ToString()}");
			await request.Send();
			var response = request.GetMasterResponse<Instance>();
			if (response.HasError()) {
				Logger.LogError($"Failed to fetch instance {identifier} from {address}: {response.GetError().GetMessage()}");
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
			await request.SetMasterUrl(address, $"/api/instances?{data.ToParams()}");
			await request.Send();
			var response = request.GetMasterResponse<SearchResponse>();
			Logger.LogDebug(request.GetResponse<string>());
			if (response.HasError()) {
				Logger.LogError($"Failed to search instances from {address}: {response.GetError().GetMessage()}");
				return null;
			}

			var instances = response.GetData();

			foreach (var user in instances.instances)
				InvokeFetch(user);

			return instances;
		}
	}
}