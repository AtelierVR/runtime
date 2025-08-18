using System;
using System.Linq;
using System.Reflection;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using Nox.Users;
using UnityEngine.Device;

namespace api.nox.discord {
	public class DiscordSystem : MainModInitializer {
		internal static MainModCoreAPI   CoreAPI;
		private         DiscordRpcClient _client;

		internal static Assembly Assembly;

		internal static IUserAPI UserAPI
			=> CoreAPI.ModAPI
				.GetMod("user")
				?.GetEntry<IUserAPI>();

		private EventSubscription _userFetchSub;

		private static string GetDllPath() {
			var mod    = CoreAPI.ModAPI.GetMod(CoreAPI.ModMetadata.GetId());
			var folder = mod.GetData("folder", "");
			if (string.IsNullOrEmpty(folder))
				throw new NullReferenceException();

			return folder + "/lib/DiscordRPC.dll";
		}

		public void OnInitializeMain(MainModCoreAPI api) {
			CoreAPI = api;

			_userFetchSub = api.EventAPI.Subscribe("user_fetch", OnUserFetched);

			var dllPath = GetDllPath();
			Assembly = Assembly.LoadFile(dllPath);

			_client = new DiscordRpcClient("1353926096487190618");
			_client.OnReady.AddListener(OnReady);
			_client.Initialize();

			UpdatePresence();
		}

		private void OnUserFetched(EventData context)
			=> UpdatePresence();

		public void OnPostInitializeMain()
			=> UpdatePresence();

		private void UpdatePresence() {
			var user      = UserAPI.GetCurrent();
			var thumbnail = user != null ? user.GetThumbnailUrl() : "";
			var display   = user != null ? user.GetDisplay() : "";
			var presence = new RichPresence {
				#if UNITY_EDITOR
				Details = "VR Game Development",
				State   = Application.isEditor && !Application.isPlaying ? "In Editor" : "In Game",
				#else
				Details = "Playing Nox",
				State   = "In Game",
				#endif
				Assets = new Assets {
					LargeImageKey  = string.IsNullOrEmpty(thumbnail) ? "default" : thumbnail,
					LargeImageText = string.IsNullOrEmpty(display) ? "Not logged" : display,
					SmallImageKey  = string.IsNullOrEmpty(thumbnail) ? "" : "default"
				}
			};

			Logger.LogDebug($"User: {user}");
			Logger.LogDebug($"display: {display}");
			Logger.LogDebug($"thumbnail: {thumbnail}");
			Logger.LogDebug($"Details: {presence.Details}");
			Logger.LogDebug($"State: {presence.State}");
			Logger.LogDebug($"LargeImageKey: {presence.Assets.LargeImageKey}");
			Logger.LogDebug($"LargeImageText: {presence.Assets.LargeImageText}");
			Logger.LogDebug($"SmallImageKey: {presence.Assets.SmallImageKey}");
			Logger.LogDebug($"SmallImageText: {presence.Assets.SmallImageText}");

			_client.SetPresence(presence);
		}

		private void OnReady(object sender, ReadyMessage msg) { }

		public void OnDisposeMain() {
			CoreAPI.EventAPI.Unsubscribe(_userFetchSub);
			_client.Dispose();
			_client = null;
		}
	}
}