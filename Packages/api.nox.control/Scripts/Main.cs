using System;
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

			var address = IPAddress.Parse(Config.Load().Get("settings.control.address", "0.0.0.0"));
			var preferredPort = Config.Load().Get("settings.control.port", 8000);
			var port = IsUsablePort(preferredPort, GetFreePort());
			
			_server = new WebSocketServer(address, port);

			_server.OnClientConnected.AddListener(OnClientConnected);
			_server.OnClientDisconnected.AddListener(OnClientDisconnected);
			_server.OnEventReceived.AddListener(OnDataReceived);

			try {
				_server.Start();
				_api.LoggerAPI.Log($"Control Server started on port {_server.GetPort()}");
			}
			catch (SocketException ex) {
				_api.LoggerAPI.LogError($"Failed to start Control Server on port {port}: {ex.Message}");
				
				// Try to get a different free port and retry
				var freePort = GetFreePort();
				if (freePort != port) {
					_api.LoggerAPI.Log($"Retrying with alternative port {freePort}...");
					_server = new WebSocketServer(address, freePort);
					_server.OnClientConnected.AddListener(OnClientConnected);
					_server.OnClientDisconnected.AddListener(OnClientDisconnected);
					_server.OnEventReceived.AddListener(OnDataReceived);
					
					try {
						_server.Start();
						_api.LoggerAPI.Log($"Control Server started on alternative port {_server.GetPort()}");
					}
					catch (SocketException retryEx) {
						_api.LoggerAPI.LogError($"Failed to start Control Server on alternative port {freePort}: {retryEx.Message}");
						_server = null;
						throw;
					}
				} else {
					_server = null;
					throw;
				}
			}
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
			try {
				if (_server != null) {
					var port = _server.GetPort();
					_server.Stop();
					_api?.LoggerAPI.Log($"Control Server stopped on port {port}");
					_server = null;
				}
			}
			catch (Exception ex) {
				_api?.LoggerAPI.LogError($"Error disposing Control Server: {ex.Message}");
			}
			finally {
				_api = null;
			}
		}

		public static int IsUsablePort(int port, int fallbackPort) {
			try {
				var listener = new TcpListener(IPAddress.Any, port);
				listener.Start();
				listener.Stop();
				return port;
			} catch (SocketException ex) {
				Logger.LogWarning($"Port {port} is not available ({ex.Message}), using fallback port {fallbackPort}");
				return fallbackPort;
			} catch (Exception ex) {
				Logger.LogWarning($"Unable to test port {port} ({ex.Message}), using fallback port {fallbackPort}");
				return fallbackPort;
			}
		}

		public static int GetFreePort() {
			try {
				var listener = new TcpListener(IPAddress.Any, 0);
				listener.Start();
				var port = ((IPEndPoint)listener.LocalEndpoint).Port;
				listener.Stop();
				return port;
			} catch (Exception ex) {
				Logger.LogError($"Failed to get free port: {ex.Message}");
				// Return a high port number as last resort
				return 8000 + new System.Random().Next(1000, 9999);
			}
		}
	}
}