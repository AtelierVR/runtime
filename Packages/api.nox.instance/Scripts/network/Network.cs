using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Network;
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

		public async UniTask<Instance> Fetch(string identifier, string from = null, CancellationToken cancellationToken = default)
			=> await Fetch(InstanceIdentifier.FromString(identifier), from, cancellationToken);

		public async UniTask<Instance> Fetch(IInstanceIdentifier identifier, string from = null, CancellationToken cancellationToken = default)
			=> await Fetch(InstanceIdentifier.FromBase(identifier), from, cancellationToken);

		public UniTask<Instance> Fetch(uint id, string from = null, CancellationToken cancellationToken = default)
			=> Fetch(id.ToString(), from, cancellationToken);

		private async UniTask<Instance> Fetch(InstanceIdentifier identifier, string from = null, CancellationToken cancellationToken = default) {
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
				Logger.LogError($"Cannot fetch instance for {identifier}: no server address provided.");
				return null;
			}

			if (address == identifier.GetServerAddress())
				identifier.Server = "::"; // Use "::" to indicate local server in the identifier

			var request = await RequestNode.To(address, $"/api/instances/{identifier.ToString()}");
			if (request == null) {
				Logger.LogError($"Failed to create request for instance {identifier}");
				return null;
			}

			await request.Send(cancellationToken);
			var response = await request.Node<Instance>(cancellationToken);
			if (response.HasError()) {
				Logger.LogError($"Failed to fetch instance {identifier} from {address}: {response.Error.Message}");
				return null;
			}

			var instance = response.Data;
			InvokeFetch(instance);
			return instance;
		}

		public async UniTask<SearchResponse> Search(SearchRequest data, string from = null, CancellationToken cancellationToken = default) {
			if (Main.NetworkAPI == null)
				return null;

			var address = from ?? Main.UserAPI?.GetCurrent()?.GetServerAddress();
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError("Cannot search instances: no server address provided.");
				return null;
			}

			var request = await RequestNode.To(address, $"/api/instances?{data.ToParams()}");
			if (request == null) {
				Logger.LogError($"Failed to create request for instance search");
				return null;
			}

			await request.Send(cancellationToken);
			var response = await request.Node<SearchResponse>(cancellationToken);
			if (response.HasError()) {
				Logger.LogError($"Failed to search instances from {address}: {response.Error.Message}");
				return null;
			}

			var instances = response.Data;

			foreach (var instance in instances.instances)
				InvokeFetch(instance);

			return instances;
		}
	}
}