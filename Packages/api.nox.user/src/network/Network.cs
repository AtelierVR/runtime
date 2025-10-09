using System;
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
			if (Main.NetworkAPI == null)
				return null;
			var address = ServerAddress;
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError("Cannot fetch current user: no server address provided.");
				return null;
			}

			var request = Main.NetworkAPI.MakeRequest();
			await request.SetMasterUrl(address, "/api/users/@me");
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
			if (Main.NetworkAPI == null)
				return null;
			var ide = UserIdentifier.FromString(identifier);
			if (ide.IsLocal())
				ide.Server = from;
			var address = from ?? CurrentUser?.GetServerAddress() ?? ServerAddress ?? ide.GetServerAddress();
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError($"Cannot fetch user {identifier}: no server address provided.");
				return null;
			}

			var request = Main.NetworkAPI.MakeRequest();
			await request.SetMasterUrl(address, $"/api/users/{ide.ToString()}");
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
			if (Main.NetworkAPI == null)
				return null;
			var address = from ?? CurrentUser?.GetServerAddress() ?? ServerAddress;
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError("Cannot search users: no server address provided.");
				return null;
			}

			var request = Main.NetworkAPI.MakeRequest();
			await request.SetMasterUrl(address, $"/api/users?{data.ToParams()}");
			await request.Send();
			var response = request.GetMasterResponse<SearchResponse>();
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
			if (Main.NetworkAPI == null)
				return false;

			var address = ServerAddress;
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError("Cannot logout: no server address provided.");
				return false;
			}

			var request = Main.NetworkAPI.MakeRequest();
			await request.SetMasterUrl(address, "/api/auth/logout");
			await request.Send();

			var response = request.GetMasterResponse<LogoutResponse>();
			if (response.HasError()) {
				Logger.LogError($"Failed to logout from {address}: {response.GetError().GetMessage()}");
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
			if (Main.NetworkAPI == null)
				return new LoginResponse { Error = "Network API is not initialized." };
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError("Cannot logout: no server address provided.");
				return new LoginResponse { Error = "No server address provided." };
			}

			var request = Main.NetworkAPI.MakeRequest();
			await request.SetMasterUrl(address, "/api/auth/login");
			request.SetBody(form.ToJson(), "application/json");
			request.SetMethod("POST");
			await request.Send();

			var response = request.GetMasterResponse<LoginResponse>();
			var login    = response.GetData();

			Logger.LogDebug($"Login response: {response.HasError()} {response.GetError()}");
			if (response.HasError()) {
				var errorInfo = response.GetError();

				return new LoginResponse {
					Error = errorInfo.GetMessage(),
					Verification = new VerificationRequired {
						Required = errorInfo.GetCode() == 20 && errorInfo.GetStatus() == 428,
						Methods  = login?.methods ?? Array.Empty<VerificationMethod>()
					}
				};
			}


			// Successful login - set current user and save config
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
			if (Main.NetworkAPI == null)
				return new IntegrityResponse { Error = "Network API is not initialized." };
			var address = ServerAddress;
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError("Cannot logout: no server address provided.");
				return new IntegrityResponse { Error = "No server address provided." };
			}

			var request = Main.NetworkAPI.MakeRequest();
			await request.SetMasterUrl(address, "/api/users/@me/integrity");
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
			if (Main.NetworkAPI == null)
				return null;

			if (string.IsNullOrEmpty(server)) {
				Logger.LogError("Cannot get token: no server provided.");
				return null;
			}

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

		public async UniTask<SendVerificationCodeResponse> SendVerificationCode(string type, string from = null) {
			if (Main.NetworkAPI == null)
				return new SendVerificationCodeResponse { success = false, message = "Network API is not initialized." };

			var address = from ?? CurrentUser?.GetServerAddress() ?? ServerAddress;
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError("Cannot send verification code: no server address provided.");
				return new SendVerificationCodeResponse { success = false, message = "No server address provided." };
			}

			var request = Main.NetworkAPI.MakeRequest();
			await request.SetMasterUrl(address, $"/api/auth/{type}/send");
			await request.Send();
			var response = request.GetMasterResponse<SendVerificationCodeResponse>();
			if (response.HasError()) {
				Logger.LogError($"Failed to send verification code from {address}: {response.GetError().GetMessage()}");
				return new SendVerificationCodeResponse { success = false, message = response.GetError().GetMessage() };
			}

			var verificationResponse = response.GetData();
			Logger.LogDebug($"Verification code send result: {verificationResponse}");
			return verificationResponse;
		}

		public async UniTask<CurrentUser> UpdateCurrentUser(UpdateCurrentUserRequest data, string from = null) {
			if (Main.NetworkAPI == null)
				return null;

			var address = from ?? CurrentUser?.GetServerAddress() ?? ServerAddress;
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError("Cannot update current user: no server address provided.");
				return null;
			}

			var request = Main.NetworkAPI.MakeRequest();
			await request.SetMasterUrl(address, "/api/users/@me");
			request.SetBody(data.ToJson(), "application/json");
			request.SetMethod("POST");
			await request.Send();

			var response = request.GetMasterResponse<CurrentUser>();
			if (response.HasError())
				return null;

			var updateResult = response.GetData();
			if (updateResult == null) {
				Logger.LogError("Failed to update current user: no data returned.");
				return null;
			}

			CurrentUser = updateResult;
			InvokeUpdate(CurrentUser);
			Logger.LogDebug($"Current user updated: {CurrentUser}");
			return CurrentUser;
		}
	}
}