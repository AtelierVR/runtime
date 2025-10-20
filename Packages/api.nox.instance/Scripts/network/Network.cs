using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using Nox.Instances;
using UnityEngine.Events;

namespace api.nox.instance.network {
	public class Network {
		private readonly UnityEvent<Instance> _fetchEvent = new();

		private void InvokeFetch(Instance instance) {
			if (instance == null) return;
			_fetchEvent.Invoke(instance);
			Main.Instance.CoreAPI.EventAPI.Emit("instance_fetch", instance);
		}

		public async UniTask<Instance> Fetch(string identifier, string from = null)
			=> await Fetch(InstanceIdentifier.FromString(identifier), from);

		public async UniTask<Instance> Fetch(IInstanceIdentifier identifier, string from = null)
			=> await Fetch(InstanceIdentifier.FromBase(identifier), from);

		public UniTask<Instance> Fetch(uint id, string from = null)
			=> Fetch(id.ToString(), from);
		
		private async UniTask<Instance> Fetch(InstanceIdentifier identifier, string from = null) {
			if (Main.NetworkAPI == null)
				return null;
			
			if (identifier == null) {
				Logger.LogError("Cannot fetch instance: identifier is null.");
				return null;
			}

			if (identifier.IsLocal())
				identifier.Server = from;
			var address = from ?? Main.UserAPI?.GetCurrent()?.GetServerAddress() ?? identifier.GetServerAddress();
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError($"Cannot fetch user for {identifier}: no server address provided.");
				return null;
			}

			if (address == identifier.GetServerAddress())
				identifier.Server = "::"; // Use "::" to indicate local server in the identifier

			var request = Main.NetworkAPI.MakeRequest();
			await request.SetMasterUrl(address, $"/api/instances/{identifier.ToString()}");
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
			if (Main.NetworkAPI == null)
				return null;

			var address = from ?? Main.UserAPI?.GetCurrent()?.GetServerAddress();
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError("Cannot search users: no server address provided.");
				return null;
			}

			var request = Main.NetworkAPI.MakeRequest();
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