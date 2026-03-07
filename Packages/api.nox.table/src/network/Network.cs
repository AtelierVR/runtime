using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Network;
using UnityEngine.Events;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.table.network {
	public class Network {
		private readonly UnityEvent<Entry> _getEvent = new();
		private readonly UnityEvent<Entry> _setEvent = new();
		private readonly UnityEvent<Entry> _deleteEvent = new();

		private void InvokeGet(Entry entry) {
			if (entry == null) return;
			_getEvent.Invoke(entry);
			Main.Instance.CoreAPI.EventAPI.Emit("table_get", entry);
		}

		private void InvokeSet(Entry entry) {
			if (entry == null) return;
			_setEvent.Invoke(entry);
			Main.Instance.CoreAPI.EventAPI.Emit("table_set", entry);
			InvokeGet(entry);
		}

		private void InvokeDelete(Entry entry) {
			if (entry == null) return;
			_deleteEvent.Invoke(entry);
			Main.Instance.CoreAPI.EventAPI.Emit("table_delete", entry);
		}

		public async UniTask<Entry> Get(string key, string from = null, CancellationToken cancellationToken = default) {

			var address = from ?? Main.UserAPI?.GetCurrent()?.GetServerAddress();
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError($"Cannot get table {key}: no server address provided.");
				return null;
			}

			var request = await RequestNode.To(address, $"/api/users/@me/tables?key={key}");
			if (request == null) {
				Logger.LogError($"Failed to find {address} for table {key}");
				return null;
			}

			await request.Send(cancellationToken);
			if (!request.Ok()) {
				Logger.LogError($"Failed to get table {key} from {address}");
				return null;
			}

			var response = new Entry {
				Key = key,
				Value = await request.Text(token: cancellationToken),
				Server = address
			};

			InvokeGet(response);
			return response;
		}

		public async UniTask<Entry> Set(string key, string value, string from = null, CancellationToken cancellationToken = default) {
			var address = from ?? Main.UserAPI?.GetCurrent()?.GetServerAddress();
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError($"Cannot set table {key}: no server address provided.");
				return null;
			}

			var request = await RequestNode.To(address, $"/api/users/@me/tables?key={key}");
			if (request == null) {
				Logger.LogError($"Failed to find {address} for table {key}");
				return null;
			}

			request.SetBody(value);
			request.method = RequestExtension.Method.POST;
			await request.Send(cancellationToken);
			if (!request.Ok()) {
				Logger.LogError($"Failed to set table {key} on {address}: {request.responseCode} {await request.Text(token: cancellationToken)}");
				return null;
			}

			var response = new Entry {
				Key = key,
				Value = value,
				Server = address
			};

			InvokeSet(response);
			return response;
		}

		public async UniTask<Entry> Delete(string key, string from = null, CancellationToken cancellationToken = default) {
			var address = from ?? Main.UserAPI?.GetCurrent()?.GetServerAddress();
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError($"Cannot delete table {key}: no server address provided.");
				return null;
			}

			var request = await RequestNode.To(address, $"/api/users/@me/tables?key={key}");
			if (request == null) {
				Logger.LogError($"Failed to find {address} for table {key}");
				return null;
			}

			request.method = RequestExtension.Method.DELETE;
			await request.Send(cancellationToken);
			if (!request.Ok()) {
				Logger.LogError($"Failed to delete table {key} on {address}");
				return null;
			}

			var response = new Entry {
				Key = key,
				Value = null,
				Server = address
			};

			InvokeDelete(response);
			return response;
		}
	}
}