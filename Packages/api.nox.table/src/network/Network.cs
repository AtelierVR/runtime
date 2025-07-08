using Cysharp.Threading.Tasks;
using UnityEngine.Events;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.table.network {
	public class Network {
		private readonly UnityEvent<Entry> _getEvent    = new();
		private readonly UnityEvent<Entry> _setEvent    = new();
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

		public async UniTask<Entry> Get(string key, string from = null) {
			if (Main.NetworkAPI == null)
				return null;

			var address = from ?? Main.UserAPI?.GetCurrent()?.GetServerAddress();
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError($"Cannot get table {key}: no server address provided.");
				return null;
			}

			var request = Main.NetworkAPI.MakeRequest();
			await request.SetMasterUrl(address, $"/api/users/@me/tables?key={key}");
			await request.Send();
			if (request.GetStatus() != 200) {
				Logger.LogError($"Failed to get table {key} from {address}");
				return null;
			}

			var response = new Entry {
				Key    = key,
				Value  = request.GetResponse<string>(),
				Server = address
			};

			InvokeGet(response);
			return response;
		}

		public async UniTask<Entry> Set(string key, string value, string from = null) {
			if (Main.NetworkAPI == null)
				return null;

			var address = from ?? Main.UserAPI?.GetCurrent()?.GetServerAddress();
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError($"Cannot set table {key}: no server address provided.");
				return null;
			}

			var request = Main.NetworkAPI.MakeRequest();
			await request.SetMasterUrl(address, $"/api/users/@me/tables?key={key}");
			request.SetBody(value);
			request.SetMethod("POST");
			await request.Send();
			if (request.GetStatus() != 200) {
				Logger.LogError($"Failed to set table {key} on {address}");
				return null;
			}

			var response = new Entry {
				Key    = key,
				Value  = value,
				Server = address
			};

			InvokeSet(response);
			return response;
		}

		public async UniTask<Entry> Delete(string key, string from = null) {
			if (Main.NetworkAPI == null)
				return null;

			var address = from ?? Main.UserAPI?.GetCurrent()?.GetServerAddress();
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError($"Cannot delete table {key}: no server address provided.");
				return null;
			}

			var request = Main.NetworkAPI.MakeRequest();
			await request.SetMasterUrl(address, $"/api/users/@me/tables?key={key}");
			request.SetMethod("DELETE");
			await request.Send();
			if (request.GetStatus() != 200) {
				Logger.LogError($"Failed to delete table {key} on {address}");
				return null;
			}

			var response = new Entry {
				Key    = key,
				Value  = null,
				Server = address
			};

			InvokeDelete(response);
			return response;
		}
	}
}