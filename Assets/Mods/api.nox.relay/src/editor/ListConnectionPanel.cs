#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using api.nox.relay.connection;
using api.nox.relay.connector;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Panels;
using Nox.CCK.Utils;
using UnityEngine.UIElements;

namespace api.nox.relay.editor {
	public class ListConnectionPanel : EditorPanelBuilder, IDisposable {
		public string GetId()
			=> "list_connections";

		public string GetName()
			=> "Relay/Connections";

		public bool IsHidden()
			=> false;

		private readonly VisualElement _root       = new();
		private          DateTime      _lastUpdate = DateTime.MinValue;

		public void OnUpdate() {
			if (DateTime.UtcNow - _lastUpdate < TimeSpan.FromSeconds(2.5)) return;
			_lastUpdate = DateTime.UtcNow;

			foreach (var connection in RelaySystem.Instance.Connections)
				UpdateConnection(_root, connection);
		}

		public ListConnectionPanel() {
			RelaySystem.OnConnectionAdded.AddListener(OnConnectionAdded);
			RelaySystem.OnConnectionRemoved.AddListener(OnConnectionRemoved);
		}

		public void Dispose() {
			RelaySystem.OnConnectionAdded.RemoveListener(OnConnectionAdded);
			RelaySystem.OnConnectionRemoved.RemoveListener(OnConnectionRemoved);
		}

		private void OnConnectionAdded(Connection connection) {
			Logger.LogDebug("Connection added: " + connection.Id);
			var list = _root.Q("list");
			if (list == null) return;
			var child = list.Children().FirstOrDefault(c => c.userData is ushort id && id == connection.Id);
			if (child != null) {
				UpdateConnection(child, connection);
				return;
			}

			child                = RelayEditor.CoreAPI.AssetAPI.GetAsset<VisualTreeAsset>("connection.uxml").CloneTree();
			child.style.flexGrow = 1;
			child.userData       = connection.Id;
			UpdateConnection(child, connection);
			list.Add(child);
		}

		private void OnConnectionRemoved(Connection connection) {
			Logger.LogDebug("Connection removed: " + connection.Id);
			var list = _root.Q("list");
			if (list == null) return;

			var child = _root.Children().FirstOrDefault(c => c.userData is ushort id && id == connection.Id);
			child?.RemoveFromHierarchy();
		}

		private void UpdateConnection(VisualElement child, Connection connection) {
			var label = child.Q<Label>("id");
			label.text = connection.Id.ToString();

			var address = child.Q<Label>("address");
			address.text = connection.Connector?.Remote()?.ToString() ?? "Unknown";

			var latency = child.Q<Label>("latency");
			latency.text = connection.Latency.ToString("0");
		}

		public VisualElement Make(Dictionary<string, object> data) {
			_root.ClearBindings();
			_root.Clear();

			var child = RelayEditor.CoreAPI.AssetAPI.GetAsset<VisualTreeAsset>("list.uxml").CloneTree();
			child.style.flexGrow = 1;
			_root.Add(child);

			_root.Q<Label>("version").text = "v" + RelayEditor.CoreAPI.ModMetadata.GetVersion();

			var connect = _root.Q<Button>("connection-button");
			connect.RegisterCallback<ClickEvent>(evt => OnClickConnection().Forget());

			foreach (var connection in RelaySystem.Instance.Connections)
				OnConnectionAdded(connection);

			return _root;
		}

		public async UniTask OnClickConnection() {
			var connect = _root.Q<Button>("connection-button");
			var address = _root.Q<TextField>("connection-address").value;
			if (!connect.enabledSelf) return;
			connect.SetEnabled(false);
			Logger.Log("Connecting to " + address);

			if (string.IsNullOrEmpty(address)) {
				UnityEditor.EditorUtility.DisplayDialog("Error", "Address is empty", "OK");
				Logger.Log("Address is empty");
				connect.SetEnabled(true);
				return;
			}

			if (!IConnector.TryParseIPEndPoint(address, out var endPoint)) {
				UnityEditor.EditorUtility.DisplayDialog("Error", "Invalid address format", "OK");
				Logger.Log("Invalid address format");
				connect.SetEnabled(true);
				return;
			}

			var connection = RelaySystem.Instance.GetByAddress(endPoint);
			if (connection != null) {
				UnityEditor.EditorUtility.DisplayDialog("Error", "Already connected", "OK");
				Logger.Log("Already connected");
				connect.SetEnabled(true);
				return;
			}

			connection = Connection.New<UdpConnector>();
			if (!await connection.Connect(endPoint.Address.ToString(), (ushort)endPoint.Port)) {
				UnityEditor.EditorUtility.DisplayDialog("Error", "Failed to connect", "OK");
				Logger.Log("Failed to connect");
				await connection.Dispose();
				connect.SetEnabled(true);
				return;
			}

			var hand = await connection.RequestHandshake();
			if (hand == null) {
				UnityEditor.EditorUtility.DisplayDialog("Error", "Failed to request handshake", "OK");
				Logger.Log("Failed to request handshake");
				await connection.Dispose();
				connect.SetEnabled(true);
				return;
			}

			Logger.Log("Connection established");
			connect.SetEnabled(true);
		}
	}
}
#endif