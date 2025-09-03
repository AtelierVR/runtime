using System.Collections.Generic;
using Nox.CCK.Utils;
using Nox.VideoPlayer;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace api.nox.videoplayer {
	public class VideoPlayerManager {
		public static readonly List<IVideoPlayer> VideoPlayers = new();

		public static readonly UnityEvent<IVideoPlayer> OnRegistered   = new();
		public static readonly UnityEvent<IVideoPlayer> OnUnRegistered = new();

		public static void Listen() {
			SceneManager.sceneUnloaded += OnSceneUnloaded;
			SceneManager.sceneLoaded   += OnSceneLoaded;
		}

		public static void UnListen() {
			SceneManager.sceneUnloaded -= OnSceneUnloaded;
			SceneManager.sceneLoaded   -= OnSceneLoaded;
		}

		private static void OnSceneUnloaded(Scene scene) {
			foreach (var player in VideoPlayers.ToArray())
				if (player.GetGameObject().scene == scene)
					UnRegister(player);
		}

		private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
			var components = scene.GetComponentsInChildren<IVideoPlayer>();
			foreach (var player in components)
				Register(player);
		}

		public static void Register(IVideoPlayer player) {
			if (player == null) return;
			if (VideoPlayers.Contains(player)) return;
			VideoPlayers.Add(player);
			Main.Instance.CoreAPI.EventAPI.Emit("video_player_registered", player);
		}

		public static void UnRegister(IVideoPlayer player) {
			if (player == null) return;
			if (!VideoPlayers.Contains(player)) return;
			VideoPlayers.Remove(player);
			Main.Instance.CoreAPI.EventAPI.Emit("video_player_unregistered", player);
		}
	}
}