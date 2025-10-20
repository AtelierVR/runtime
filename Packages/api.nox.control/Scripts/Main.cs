using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using api.nox.control.handlers;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using Nox.SDK.Control;

namespace api.nox.control {
	public class Main : IMainModInitializer {
		private WebSocketServer _server;
		private MainModCoreAPI  _api;

		public void OnInitializeMain(MainModCoreAPI api) {
			_api = api;
			Reload();
		}

		private void Reload() {
			if (_server != null) {
				_server.Stop();
				_server = null;
			}

			_server = new WebSocketServer(
				IPAddress.Parse(Config.Load().Get("settings.control.address", "0.0.0.0")),
				Config.Load().Get("settings.control.port", IsUsablePort(8000, GetFreePort()))
			);

			_server.OnClientConnected.AddListener(OnClientConnected);
			_server.OnClientDisconnected.AddListener(OnClientDisconnected);
			_server.OnEventReceived.AddListener(OnDataReceived);

			_server.Start();
			_api.LoggerAPI.Log($"Control Server started on port {_server.GetPort()}");
		}

		private void OnDataReceived(IClient arg0, string arg1, params object[] arg2) {
			List<object> data = new() { arg0, arg1 };
			data.AddRange(arg2);
			_api.EventAPI.Emit("control:data", data.ToArray());
			ConfigHandler.Handle(arg0, arg1, arg2);
			HierarchyHandler.Handle(arg0, arg1, arg2);
			#if UNITY_EDITOR
			EditorHandler.Handle(arg0, arg1, arg2);
			#endif
		}

		private void OnClientDisconnected(IClient arg0) {
			_api.LoggerAPI.Log($"Client disconnected: {arg0.GetEndPoint()}");
			_api.EventAPI.Emit("control:disconnected", arg0);
		}

		private void OnClientConnected(IClient arg0) {
			_api.LoggerAPI.Log($"Client connected: {arg0.GetEndPoint()}");
			_api.EventAPI.Emit("control:connected", arg0);
		}

		public void OnDisposeMain() {
			_server.Stop();
			_api.LoggerAPI.Log($"Control Server stopped on port {_server.GetPort()}");
			_server = null;
			_api    = null;
		}

		public static int IsUsablePort(int port, int fallbackPort) {
			try {
				var listener = new TcpListener(IPAddress.Loopback, port);
				listener.Start();
				listener.Stop();
				return port;
			} catch {
				return fallbackPort;
			}
		}

		public static int GetFreePort() {
			var listener = new TcpListener(IPAddress.Loopback, 0);
			listener.Start();
			var port = ((IPEndPoint)listener.LocalEndpoint).Port;
			listener.Stop();
			return port;
		}
	}
}