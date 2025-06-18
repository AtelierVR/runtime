using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;
using System.Collections.Generic;
using Nox.CCK.Avatars;
using Nox.CCK.Worlds;

#if UNITY_EDITOR
using Nox.CCK.Utils;
using Nox.ModLoader.Mods;
using UnityEditor;
using UnityEngine.UIElements;
#endif

namespace Nox.ModLoader {
	public class StartupManager {
		private static bool _isLoaded;
		private static bool _initializing = false;

		#if UNITY_EDITOR

		[MenuItem("Nox/ModLoader/Reload Mods")]
		private static void ReloadMods()
			=> AsyncReloadMods().Forget();

		private static bool AutoStart {
			get => Config.LoadEditor().Get("auto_start", true);
			set {
				var config = Config.LoadEditor();
				config.Set("auto_start", value);
				config.Save();
			}
		}

		[MenuItem("Nox/Play Mode/Auto Start Enable")]
		private static void AutoStartEnable() {
			AutoStart = true;
			Logger.Log("Auto Start Enabled...");
		}

		[MenuItem("Nox/Play Mode/Auto Start Disable")]
		private static void AutoStartDisable() {
			AutoStart = false;
			Logger.Log("Auto Start Disabled...");
		}

		[UnityEditor.Callbacks.DidReloadScripts]
		private static void OnScriptsReloaded()
			=> AsyncInitialize().Forget();

		private static bool _isReloading;

		private static async UniTask AsyncReloadMods() {
			if (_isReloading) return;
			_isReloading = true;

			if (Application.isPlaying) {
				Logger.LogError("Cannot reload mods while playing...");
				_isReloading = false;
				return;
			}

			Logger.Log("Reloading Mods...");

			// open progress bar window
			DisplayProgressBar("Reloading Mods", "Unloading Mods...", 0.0f);

			var mods = ModManager.Mods;

			foreach (var mod in ModManager.Mods)
				await mod.SendPreDispose();

			foreach (var mod in ModManager.Mods)
				await mod.SendDispose();

			for (var i = 0; i < mods.Count; i++) {
				var mod = mods[i];
				EditorUtility.DisplayProgressBar(
					"Reloading Mods",
					$"Unloading Mod {mod.Metadata.GetId()}@{mod.Metadata.GetVersion()}...", (float)i / mods.Count
				);
				await mod.Unload();
			}

			ModManager.Mods.Clear();

			DisplayProgressBar("Reloading Mods", "Discovering Mods...", -1.0f);

			var results = await ModManager.LoadMods();

			foreach (var result in results.Results)
				if (result.IsError)
					Logger.LogError(result.Message);
				else if (result.IsWarning)
					Logger.LogWarning(result.Message);
				else Logger.Log(result.Message);

			DisplayProgressBar("Reloading Mods", "Enabling Mods...", 0.0f);

			for (var i = 0; i < results.Mods.Length; i++) {
				var mod = results.Mods[i];
				DisplayProgressBar(
					"Reloading Mods",
					$"Enabling Mod {mod.Metadata.GetId()}@{mod.Metadata.GetVersion()}...",
					(float)i / results.Mods.Length
				);
				mod.EnableMain();
				mod.EnableEditor();
			}

			foreach (var mod in results.Mods)
				await mod.SendInitialize();

			foreach (var mod in results.Mods)
				await mod.SendPostInitialize();

			_isReloading = false;

			Logger.Log("Mods Reloaded...");

			ClearProgressBar();
		}

		private static void DisplayProgressBar(string title, string info, float progress)
			=> EditorUtility.DisplayProgressBar(title, info, progress);

		private static void ClearProgressBar()
			=> EditorUtility.ClearProgressBar();

		private static void OnUpdateEditor() {
			if (Application.isPlaying || _isReloading) return;
			foreach (var mod in ModManager.Mods)
				mod.SendUpdate();
		}

		private static void OnPlayModeStateChanged(PlayModeStateChange state, ResultLoadInfos resultInfos) {
			if (state == PlayModeStateChange.EnteredPlayMode)
				StartupPlayerLoop.Setup(resultInfos);
		}

		#else
        private static void DisplayProgressBar(string title, string info, float progress)
        {
        }

        private static void ClearProgressBar()
        {
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void OnBeforeSceneLoad() => AsyncInitialize().Forget();
		#endif


		private static async UniTaskVoid AsyncInitialize() {
			#if UNITY_EDITOR
			List<string> types = new();
			if (EditorApplication.isCompiling)
				types.Add("compiling");
			if (EditorApplication.isPlaying) {
				types.Add("playing");
				EditorApplication.isPlaying = false;
				return;
			}

			if (EditorApplication.isPaused)
				types.Add("paused");
			if (EditorApplication.isRemoteConnected)
				types.Add("remote connected");
			if (EditorApplication.isTemporaryProject)
				types.Add("temporary project");

			Logger.Log($"Editor is [{string.Join(", ", types)}]...");
			if (types.Count > 0)
				return;
			#endif

			if (_initializing) return;
			_initializing = true;

			DisplayProgressBar("Loading Mods", "Discovering Mods...", -1.0f);

			var resultinfos = await ModManager.LoadMods();

			Logger.Log($"Executing Editor as [{(Application.isConsolePlatform ? "Server" : "Client")}]...");
			Logger.LogDebug($"{resultinfos.Mods.Length} mods loaded:");
			foreach (var mod in resultinfos.Mods)
				Logger.LogDebug($"- {mod.Metadata.GetId()}@{mod.Metadata.GetVersion()}");

			foreach (var result in resultinfos.Results)
				if (result.IsError)
					Logger.LogError(result.Message);
				else if (result.IsWarning)
					Logger.LogWarning(result.Message);
				else Logger.Log(result.Message);

			DisplayProgressBar("Loading Mods", "Enabling Mods...", 0.0f);

			for (var i = 0; i < resultinfos.Mods.Length; i++) {
				var mod = resultinfos.Mods[i];
				DisplayProgressBar(
					"Loading Mods",
					$"Enabling Mod {mod.Metadata.GetId()}@{mod.Metadata.GetVersion()}...",
					(float)(i + 1) / resultinfos.Mods.Length
				);
				mod.EnableMain();
				#if UNITY_EDITOR
				mod.EnableEditor();
				#endif
			}

			DisplayProgressBar("Loading Mods", "Initializing Mods...", 0.0f);

			for (var i = 0; i < resultinfos.Mods.Length; i++) {
				var mod = resultinfos.Mods[i];
				DisplayProgressBar(
					"Loading Mods",
					$"Initializing Mod {mod.Metadata.GetId()}@{mod.Metadata.GetVersion()}...",
					(float)(i + 1) / resultinfos.Mods.Length
				);
				await mod.SendInitialize();
			}

			for (var i = 0; i < resultinfos.Mods.Length; i++) {
				var mod = resultinfos.Mods[i];
				DisplayProgressBar(
					"Loading Mods",
					$"Post-Initializing Mod {mod.Metadata.GetId()}@{mod.Metadata.GetVersion()}...",
					(float)(i + 1) / resultinfos.Mods.Length
				);
				await mod.SendPostInitialize();
			}

			Logger.Log("Mods Loaded...");
			ClearProgressBar();

			#if UNITY_EDITOR
			EditorApplication.update               += OnUpdateEditor;
			EditorApplication.playModeStateChanged += state => OnPlayModeStateChanged(state, resultinfos);
			if (EditorApplication.isPlaying)
				OnPlayModeStateChanged(PlayModeStateChange.EnteredPlayMode, resultinfos);
			#else
            StartupPlayerLoop.Setup(resultinfos);
			#endif
		}

		private static async UniTask OnExitingPlayMode(ResultLoadInfos resultInfos) {
			Logger.Log("Exiting PlayMode... Disabling Mods...");

			if (!_isLoaded) {
				Logger.Log("Skipping disabling mods because game was not loaded...");
				return;
			}

			DisplayProgressBar("Exiting PlayMode", "Disabling Mods...", 0.0f);

			for (var i = 0; i < resultInfos.Mods.Length; i++) {
				var mod = resultInfos.Mods[i];
				DisplayProgressBar(
					"Exiting PlayMode",
					$"Disabling Mod {mod.Metadata.GetId()}@{mod.Metadata.GetVersion()}...",
					(float)(i + 1) / resultInfos.Mods.Length
				);
				mod.DisableClient();
				mod.DisableServer();
			}

			DisplayProgressBar("Exiting PlayMode", "Pre-Disposing Mods...", 0.0f);

			for (var i = 0; i < resultInfos.Mods.Length; i++) {
				var mod = resultInfos.Mods[resultInfos.Mods.Length - 1 - i];
				DisplayProgressBar(
					"Exiting PlayMode",
					$"Pre-Disposing Mod {mod.Metadata.GetId()}@{mod.Metadata.GetVersion()}...",
					(float)(i + 1) / resultInfos.Mods.Length
				);
				await mod.SendPreDispose();
			}

			DisplayProgressBar("Exiting PlayMode", "Disposing Mods...", 0.0f);

			for (var i = 0; i < resultInfos.Mods.Length; i++) {
				var mod = resultInfos.Mods[resultInfos.Mods.Length - 1 - i];
				DisplayProgressBar(
					"Exiting PlayMode",
					$"Disposing Mod {mod.Metadata.GetId()}@{mod.Metadata.GetVersion()}...",
					(float)(i + 1) / resultInfos.Mods.Length
				);
				await mod.SendDispose();
			}

			// clearing 

			DisplayProgressBar("Exiting PlayMode", "Clearing Mods...", 0.0f);

			for (var i = 0; i < resultInfos.Mods.Length; i++) {
				var mod = resultInfos.Mods[resultInfos.Mods.Length - 1 - i];
				DisplayProgressBar(
					"Exiting PlayMode",
					$"Clearing Mod {mod.Metadata.GetId()}@{mod.Metadata.GetVersion()}...",
					(float)(i + 1) / resultInfos.Mods.Length
				);
				mod.ClearClient();
				mod.ClearServer();
			}

			Logger.Log("Mods Disabled...");
			ClearProgressBar();
		}

		#if UNITY_EDITOR
		private enum WantsToLoad : int {
			None = 0,
			Yes  = 1,
			No   = 2
		}

		private static WantsToLoad WantsTo {
			get
				=> EditorPrefs.GetInt("Nox.ModLoader.WantsToLoad", (int)WantsToLoad.None)
					switch {
						0 => WantsToLoad.None,
						1 => WantsToLoad.Yes,
						2 => WantsToLoad.No,
						_ => WantsToLoad.None
					};
			set => EditorPrefs.SetInt("Nox.ModLoader.WantsToLoad", (int)value);
		}

		[MenuItem("Nox/Play Mode/Wants To Load/Force Yes")]
		public static void WantsToLoadYes() {
			WantsTo = WantsToLoad.Yes;
			Logger.Log("Wants to load set to Yes...");
		}

		[MenuItem("Nox/Play Mode/Wants To Load/Force No")]
		public static void WantsToLoadNo() {
			WantsTo = WantsToLoad.No;
			Logger.Log("Wants to load set to No...");
		}

		[MenuItem("Nox/Play Mode/Wants To Load/Ask every time")]
		public static void WantsToLoadAsk() {
			WantsTo = WantsToLoad.None;
			Logger.Log("Wants to load set to None...");
		}
		#endif

		private static async UniTask OnEnteredPlayMode(ResultLoadInfos resultInfos) {
			Logger.Log("Entered PlayMode... Enabling Mods...");

			#if UNITY_EDITOR
			// check if auto start is disabled
			if (!AutoStart) {
				Logger.LogWarning("Auto Start Disabled...");
				return;
			}

			Mod blockerMod = null;

			// Send can_load event to all mods
			foreach (var mod in resultInfos.Mods) {
				mod.CoreAPI.LocalEventAPI.Emit("mod_can_load", mod, new Action<object[]>(Action));
				continue;

				void Action(object[] obj) {
					if (obj.Length > 0 && obj[0] is false) {
						blockerMod = mod;
						Logger.LogWarning($"Mod {mod.Metadata.GetId()}@{mod.Metadata.GetVersion()} blocked loading the game.");
					} else Logger.LogDebug($"Mod {mod.Metadata.GetId()}@{mod.Metadata.GetVersion()} allowed loading the game.");
				}
			}

			// If any mod returned false, handle based on WantsTo setting
			if (blockerMod != null) {
				switch (WantsTo) {
					case WantsToLoad.Yes:
						Logger.Log("User wants to load the game (forced)...");
						break;
					case WantsToLoad.No:
						Logger.Log("User does not want to load the game...");
						return;
					case WantsToLoad.None:
					default: {
						var result = EditorUtility.DisplayDialog(
							"Mod Loader",
							$"Mod {blockerMod.Metadata.GetId()}@{blockerMod.Metadata.GetVersion()} blocked loading the game. Do you want to continue?",
							"Yes, continue",
							"No, cancel"
						);
						if (!result) {
							Logger.Log("User does not want to load the game...");
							return;
						}

						Logger.Log("User wants to load the game (asked)...");

						break;
					}
				}
			}

			// disabling all keybinds of unityeditor to prevent conflicts
			// ShortcutManager.instance.activeProfileId = "Play";
			#endif

			_isLoaded = true;

			DisplayProgressBar("Entered PlayMode", "Enabling Mods...", 0.0f);
			for (var i = 0;
			     i < resultInfos.Mods.Length;
			     i++) {
				var mod = resultInfos.Mods[i];
				DisplayProgressBar(
					"Entered PlayMode",
					$"Enabling Mod {mod.Metadata.GetId()}@{mod.Metadata.GetVersion()}...",
					(float)(i + 1) / resultInfos.Mods.Length
				);
				if (Application.isConsolePlatform)
					mod.EnableServer();
				else mod.EnableClient();
			}

			DisplayProgressBar("Entered PlayMode", "Initializing Mods...", 0.0f);
			for (var i = 0;
			     i < resultInfos.Mods.Length;
			     i++) {
				var mod = resultInfos.Mods[i];
				DisplayProgressBar(
					"Entered PlayMode",
					$"Initializing Mod {mod.Metadata.GetId()}@{mod.Metadata.GetVersion()}...",
					(float)(i + 1) / resultInfos.Mods.Length
				);
				await mod.SendInitialize();
			}

			DisplayProgressBar("Entered PlayMode", "Post-Initializing Mods...", 0.0f);
			for (var i = 0;
			     i < resultInfos.Mods.Length;
			     i++) {
				var mod = resultInfos.Mods[i];
				DisplayProgressBar(
					"Entered PlayMode",
					$"Post-Initializing Mod {mod.Metadata.GetId()}@{mod.Metadata.GetVersion()}...",
					(float)(i + 1) / resultInfos.Mods.Length
				);
				await mod.SendPostInitialize();
			}

			Logger.Log("Mods Enabled...");
			ClearProgressBar();
		}


		public class StartupPlayerLoop : MonoBehaviour {
			private static StartupPlayerLoop _instance;
			private        ResultLoadInfos   _resultInfos;

			public static void Setup(ResultLoadInfos resultInfos) {
				if (_instance)
					throw new Exception("StartupPlayerLoop already exists...");
				var go = new GameObject();
				_instance = go.AddComponent<StartupPlayerLoop>();
				go.name   = $"[{_instance.GetType().Name}]";
				#if UNITY_EDITOR
				EditorGUIUtility.SetIconForObject(_instance, Resources.Load<Texture2D>("Nox.CCK.Icon"));
				#endif
				_instance._resultInfos = resultInfos;
				DontDestroyOnLoad(go);
			}

			private async void OnApplicationQuit() {
				try {
					Logger.Log("Application Quit...");
					await OnExitingPlayMode(_resultInfos);
				} catch {
					// ignored
				}
			}

			private async void Start() {
				try {
					Logger.Log("StartupPlayerLoop Started...");
					await OnEnteredPlayMode(_resultInfos);
				} catch {
					// ignored
				}
			}

			private void Update() {
				foreach (var mod in ModManager.Mods)
					mod.SendUpdate();
			}

			private void LateUpdate() {
				foreach (var mod in ModManager.Mods)
					mod.SendLateUpdate();
			}

			private void FixedUpdate() {
				foreach (var mod in ModManager.Mods)
					mod.SendFixedUpdate();
			}
		}
	}
}