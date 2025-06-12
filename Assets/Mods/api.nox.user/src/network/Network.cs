using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Nox.CCK.Utils;
using UnityEngine.Events;

namespace api.nox.user.network {
	public class Network {
		public CurrentUser CurrentUser;

		public string ServerAddress {
			get => Config.Load().Get<string>("server");
			set {
				var config = Config.Load();
				config.Set("server", value);
				config.Save();
			}
		}

		public async UniTask<CurrentUser> FetchCurrent() {
			if (Main.Instance.NetworkAPI == null)
				return null;
			var address = ServerAddress;
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError("Cannot fetch current user: no server address provided.");
				return null;
			}

			var request = Main.Instance.NetworkAPI.MakeRequest();
			request.SetMasterUrl(address, "/api/users/@me");
			await request.Send();
			var response = request.GetMasterResponse<CurrentUser>();
			if (response.HasError()) {
				Logger.LogError($"Failed to fetch current user from {address}: {response.GetError().GetMessage()}");
				return null;
			}

			CurrentUser = response.GetData();
			InvokeUpdate(CurrentUser);
			return CurrentUser;
		}

		private readonly UnityEvent<User>        _fetchEvent  = new();
		private readonly UnityEvent<CurrentUser> _updateEvent = new();
		private readonly UnityEvent<CurrentUser> _logoutEvent = new();
		private readonly UnityEvent<CurrentUser> _loginEvent  = new();

		private void InvokeFetch(User user) {
			if (user == null) return;
			_fetchEvent.Invoke(user);
			Main.Instance.CoreAPI.EventAPI.Emit("user_fetch", user);
		}

		private void InvokeUpdate(CurrentUser user) {
			_updateEvent.Invoke(user);
			Main.Instance.CoreAPI.EventAPI.Emit("user_update", user);
			InvokeFetch(user);
		}

		public void InvokeLogout(CurrentUser user) {
			_logoutEvent.Invoke(user);
			Main.Instance.CoreAPI.EventAPI.Emit("user_logout", user);
			InvokeUpdate(null);
		}

		public void InvokeLogin(CurrentUser user) {
			_loginEvent.Invoke(user);
			Main.Instance.CoreAPI.EventAPI.Emit("user_login", user);
			InvokeUpdate(user);
		}

		public UniTask<User> Fetch(UserIdentifier identifier, string from = null)
			=> Fetch(identifier.ToString(), from);

		public UniTask<User> Fetch(uint id, string from = null)
			=> Fetch(id.ToString(), from);

		public async UniTask<User> Fetch(string identifier, string from = null) {
			if (Main.Instance.NetworkAPI == null)
				return null;
			var ide = UserIdentifier.FromString(identifier);
			if (ide.IsLocal())
				ide.Server = from;
			var address = from ?? CurrentUser?.GetServerAddress() ?? ServerAddress ?? ide.GetServerAddress();
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError($"Cannot fetch user {identifier}: no server address provided.");
				return null;
			}

			var request = Main.Instance.NetworkAPI.MakeRequest();
			request.SetMasterUrl(address, $"/api/users/{ide.ToString()}");
			await request.Send();
			var response = request.GetMasterResponse<User>();
			if (response.HasError()) {
				Logger.LogError($"Failed to fetch user {identifier} from {address}: {response.GetError().GetMessage()}");
				return null;
			}

			var user = response.GetData();
			InvokeFetch(user);
			return user;
		}

		public async UniTask<SearchResponse> Search(SearchRequest data, string from = null) {
			if (Main.Instance.NetworkAPI == null)
				return null;
			var address = from ?? CurrentUser?.GetServerAddress() ?? ServerAddress;
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError("Cannot search users: no server address provided.");
				return null;
			}

			var request = Main.Instance.NetworkAPI.MakeRequest();
			request.SetMasterUrl(address, $"/api/users?{data.ToParams()}");
			await request.Send();
			var response = request.GetMasterResponse<SearchResponse>();
			Logger.LogDebug(request.GetResponse<string>());
			if (response.HasError()) {
				Logger.LogError($"Failed to search users from {address}: {response.GetError().GetMessage()}");
				return null;
			}

			var users = response.GetData();

			foreach (var user in users.users)
				InvokeFetch(user);

			return users;
		}

		public async UniTask<bool> Logout() {
			if (Main.Instance.NetworkAPI == null)
				return false;

			var address = ServerAddress;
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError("Cannot logout: no server address provided.");
				return false;
			}

			var request = Main.Instance.NetworkAPI.MakeRequest();
			request.SetMasterUrl(address, "/api/auth/logout");
			await request.Send();
			var response = request.GetMasterResponse<LogoutResponse>();
			if (response.HasError()) {
				Logger.LogError($"Failed to logout from {address}: {response.GetError().GetMessage()}");
				return false;
			}

			var logoutResponse = response.GetData();
			if (!logoutResponse.success) {
				Logger.LogError($"Logout failed from {address}: {response.GetError().GetMessage()}");
				return false;
			}

			CurrentUser   = null;
			ServerAddress = null;
			var config = Config.Load();
			config.Remove(new[] { "server", address, "token" });
			config.Remove(new[] { "server", address, "expires" });
			config.Remove(new[] { "server", address, "user_id" });
			config.Remove(new[] { "server", address, "integrity" });
			config.Save();
			InvokeLogout(CurrentUser);
			return true;
		}

		public void Dispose() {
			CurrentUser = null;
		}

		[Serializable]
		internal class LogoutResponse {
			public bool success;
		}

		public async UniTask<LoginResponse> Login(LoginRequest form, string address) {
			if (Main.Instance.NetworkAPI == null)
				return new LoginResponse { Error = "Network API is not initialized." };
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError("Cannot logout: no server address provided.");
				return new LoginResponse { Error = "No server address provided." };
			}

			var request = Main.Instance.NetworkAPI.MakeRequest();
			request.SetMasterUrl(address, "/api/auth/login");
			request.SetBody(form.ToJson(), "application/json");
			request.SetMethod("POST");
			await request.Send();
			Logger.LogDebug(request.GetResponse<string>());
			var response = request.GetMasterResponse<LoginResponse>();
			Logger.LogDebug("data" + response.GetData());
			Logger.LogDebug("err"  + response.GetError());
			if (response.HasError()) {
				Logger.LogError($"Failed to login to {address}: {response.GetError().GetMessage()}");
				return new LoginResponse { Error = response.GetError().GetMessage() };
			}

			var login = response.GetData();
			if (login.IsError()) {
				Logger.LogError($"Login error for {address}: {login.Error}");
				return login;
			}

			CurrentUser   = login.user;
			ServerAddress = login.user.server;
			var config = Config.Load();
			config.Set(new[] { "servers", login.user.server, "token" }, login.token);
			config.Set(new[] { "servers", login.user.server, "expires" }, login.expires);
			config.Set(new[] { "servers", login.user.server, "user_id" }, login.user.id);
			config.Remove(new[] { "servers", login.user.server, "integrity" });
			config.Save();
			InvokeLogin(login.user);
			return login;
		}

		public async UniTask<IntegrityResponse> CreateIntegrity(string server) {
			if (Main.Instance.NetworkAPI == null)
				return new IntegrityResponse { Error = "Network API is not initialized." };
			var address = ServerAddress;
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError("Cannot logout: no server address provided.");
				return new IntegrityResponse { Error = "No server address provided." };
			}

			var request = Main.Instance.NetworkAPI.MakeRequest();
			request.SetMasterUrl(address, "/api/users/@me/integrity");
			request.SetBody(
				new JObject {
					["address"] = server,
				}.ToString(),
				"application/json"
			);
			request.SetMethod("PUT");
			await request.Send();
			var response = request.GetMasterResponse<IntegrityResponse>();
			if (response.HasError()) {
				Logger.LogError($"Failed to create integrity from {address} for {server}: {response.GetError().GetMessage()}");
				return new IntegrityResponse { Error = response.GetError().GetMessage() };
			}

			var integrity = response.GetData();
			if (integrity.IsError()) {
				Logger.LogError($"Integrity creation error from {address} for {server}: {integrity.Error}");
				return integrity;
			}

			var config = Config.Load();
			config.Set(new[] { "servers", address, "integrity", server, "token" }, integrity.token);
			config.Set(new[] { "servers", address, "integrity", server, "expires" }, integrity.expires);
			config.Save();
			return integrity;
		}

		public async UniTask<AuthToken> GetToken(string server) {
			if (Main.Instance.NetworkAPI == null)
				return null;

			var address = ServerAddress;
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError("Cannot get token: no server address provided.");
				return null;
			}

			var config = Config.Load();

			if (server == address) {
				if (!config.Has(new[] { "servers", address, "token" })) return null;
				var expires = config.Get(new[] { "servers", address, "expires" }, long.MinValue);
				if (expires > DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
					return new AuthToken {
						Token     = config.Get<string>(new[] { "servers", address, "token" }),
						Integrity = false
					};
				return null;
			}

			if (config.Has(new[] { "servers", address, "integrity", server, "token" })) {
				var expires = config.Get(new[] { "servers", address, "integrity", server, "expires" }, long.MinValue);
				if (expires > DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
					return new AuthToken {
						Token     = config.Get<string>(new[] { "servers", address, "integrity", server, "token" }),
						Integrity = true
					};
			}

			var result = await CreateIntegrity(server);
			if (result != null && !result.IsExpired()) {
				config.Set(new[] { "servers", address, "integrity", server, "token" }, result.token);
				config.Set(new[] { "servers", address, "integrity", server, "expires" }, result.expires);
				config.Save();
				return new AuthToken {
					Token     = result.token,
					Integrity = true
				};
			}

			return null;
		}
	}
}