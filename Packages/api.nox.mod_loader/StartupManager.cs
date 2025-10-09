using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;
using System.Collections.Generic;
using Nox.ModLoader.EntryPoints;
using Nox.CCK.Utils;
using Nox.ModLoader.Mods;

#if UNITY_EDITOR
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
			mods.Reverse();

			foreach (var mod in ModManager.Mods)
				await mod.PreDispose();

			foreach (var mod in ModManager.Mods)
				await mod.Dispose();

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
				mod.GetEntry(EntryPoint.MainEntry).Enable();
				mod.GetEntry(EntryPoint.EditorEntry).Enable();
			}

			foreach (var mod in results.Mods)
				await mod.Initialize();

			foreach (var mod in results.Mods)
				await mod.PostInitialize();

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
				mod.Update();
		}

		private static void OnPlayModeStateChanged(PlayModeStateChange state, ResultLoadInfos resultInfos) {
			if (state == PlayModeStateChange.EnteredPlayMode)
				StartupPlayerLoop.Setup(resultInfos);
			else if (state == PlayModeStateChange.ExitingPlayMode)
				AsyncReloadMods().Forget();
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
			// Force background mode to true (for updating mods in background like sockets with Update method)
			Application.runInBackground = true;

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

			var loaded = await ModManager.LoadMods();

			Logger.Log($"Executing Editor as [{(Application.isConsolePlatform ? "Server" : "Client")}]...");
			Logger.LogDebug($"{loaded.Mods.Length} mods loaded:");
			foreach (var mod in loaded.Mods)
				Logger.LogDebug($"- {mod.Metadata.GetId()}@{mod.Metadata.GetVersion()}");

			foreach (var result in loaded.Results)
				if (result.IsError)
					Logger.LogError(result.Message);
				else if (result.IsWarning)
					Logger.LogWarning(result.Message);
				else Logger.Log(result.Message);

			DisplayProgressBar("Loading Mods", "Enabling Mods...", 0.0f);

			for (var i = 0; i < loaded.Mods.Length; i++) {
				var mod = loaded.Mods[i];
				DisplayProgressBar(
					"Loading Mods",
					$"Enabling Mod {mod.Metadata.GetId()}@{mod.Metadata.GetVersion()}...",
					(float)(i + 1) / loaded.Mods.Length
				);
				mod.GetEntry(EntryPoint.MainEntry)?.Enable();
				#if UNITY_EDITOR
				mod.GetEntry(EntryPoint.EditorEntry)?.Enable();
				#endif
			}

			DisplayProgressBar("Loading Mods", "Initializing Mods...", 0.0f);

			for (var i = 0; i < loaded.Mods.Length; i++) {
				var mod = loaded.Mods[i];
				DisplayProgressBar(
					"Loading Mods",
					$"Initializing Mod {mod.Metadata.GetId()}@{mod.Metadata.GetVersion()}...",
					(float)(i + 1) / loaded.Mods.Length
				);
				await mod.Initialize();
			}

			for (var i = 0; i < loaded.Mods.Length; i++) {
				var mod = loaded.Mods[i];
				DisplayProgressBar(
					"Loading Mods",
					$"Post-Initializing Mod {mod.Metadata.GetId()}@{mod.Metadata.GetVersion()}...",
					(float)(i + 1) / loaded.Mods.Length
				);
				await mod.PostInitialize();
			}

			Logger.Log("Mods Loaded...");
			ClearProgressBar();

			#if UNITY_EDITOR
			EditorApplication.update               += OnUpdateEditor;
			EditorApplication.playModeStateChanged += state => OnPlayModeStateChanged(state, loaded);
			if (EditorApplication.isPlaying)
				OnPlayModeStateChanged(PlayModeStateChange.EnteredPlayMode, loaded);
			#else
            StartupPlayerLoop.Setup(loaded);
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
				mod.GetEntry(EntryPoint.ClientEntry)?.Disable();
				mod.GetEntry(EntryPoint.ServerEntry)?.Disable();
			}

			DisplayProgressBar("Exiting PlayMode", "Pre-Disposing Mods...", 0.0f);

			for (var i = 0; i < resultInfos.Mods.Length; i++) {
				var mod = resultInfos.Mods[resultInfos.Mods.Length - 1 - i];
				DisplayProgressBar(
					"Exiting PlayMode",
					$"Pre-Disposing Mod {mod.Metadata.GetId()}@{mod.Metadata.GetVersion()}...",
					(float)(i + 1) / resultInfos.Mods.Length
				);
				await mod.PreDispose();
			}

			DisplayProgressBar("Exiting PlayMode", "Disposing Mods...", 0.0f);

			for (var i = 0; i < resultInfos.Mods.Length; i++) {
				var mod = resultInfos.Mods[resultInfos.Mods.Length - 1 - i];
				DisplayProgressBar(
					"Exiting PlayMode",
					$"Disposing Mod {mod.Metadata.GetId()}@{mod.Metadata.GetVersion()}...",
					(float)(i + 1) / resultInfos.Mods.Length
				);
				await mod.Dispose();
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

		[MenuItem("Nox/ModLoader/Debug/One Runtime (Default)")]
		private static void SetOneRuntimeMode() {
			OneRuntime = true;
			Logger.Log("Component Mode set to One Component (StartupManager for all mods)...");
		}

		[MenuItem("Nox/ModLoader/Debug/Individual Components")]
		private static void SetIndividualComponentsMode() {
			OneRuntime = false;
			Logger.Log("Component Mode set to Individual Components (ModComponent per mod)...");
		}
		#endif

		private static bool OneRuntime {
			get => Config.Load().Get("settings.debug.one_runtime", true);
			set {
				var config = Config.Load();
				config.Set("settings.debug.one_runtime", value);
				config.Save();
			}
		}

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
					mod.GetEntry(EntryPoint.ServerEntry)?.Enable();
				else mod.GetEntry(EntryPoint.ClientEntry)?.Enable();
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
				await mod.Initialize();
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
				await mod.PostInitialize();
			}

			Logger.Log("Mods Enabled...");
			ClearProgressBar();
		}


		public class ModComponent : MonoBehaviour {
			private Mod _mod;

			public static ModComponent Create(Mod mod) {
				var go        = new GameObject();
				var component = go.AddComponent<ModComponent>();
				go.name = $"[ModComponent:{mod.Metadata.GetId()}]";
				#if UNITY_EDITOR
				EditorGUIUtility.SetIconForObject(component, Resources.Load<Texture2D>("Nox.CCK.Icon"));
				#endif
				component._mod = mod;
				DontDestroyOnLoad(go);
				return component;
			}

			private void Update() {
				_mod?.Update();
			}

			private void LateUpdate() {
				_mod?.LateUpdate();
			}

			private void FixedUpdate() {
				_mod?.FixedUpdate();
			}
		}

		public class StartupPlayerLoop : MonoBehaviour {
			private static StartupPlayerLoop _instance;
			private        ResultLoadInfos   _resultInfos;
			private        bool              _useOneComponent;

			public static void Setup(ResultLoadInfos resultInfos) {
				var useOneComponent = OneRuntime;

				if (useOneComponent) {
					// Mode original : un seul composant pour tous les mods
					Logger.Log("Using One Component mode (StartupManager handles all mods)...");
					if (_instance)
						throw new Exception("StartupPlayerLoop already exists...");
					var go = new GameObject();
					_instance = go.AddComponent<StartupPlayerLoop>();
					go.name   = $"[{_instance.GetType().Name}]";
					#if UNITY_EDITOR
					EditorGUIUtility.SetIconForObject(_instance, Resources.Load<Texture2D>("Nox.CCK.Icon"));
					#endif
					_instance._resultInfos     = resultInfos;
					_instance._useOneComponent = true;
					DontDestroyOnLoad(go);
				} else {
					// Nouveau mode : un composant par mod
					Logger.Log($"Using Individual Components mode (ModComponent per mod, {resultInfos.Mods.Length} components will be created)...");
					foreach (var mod in resultInfos.Mods) {
						ModComponent.Create(mod);
					}

					// Créer quand même un StartupPlayerLoop pour gérer les événements globaux
					// mais sans les updates des mods
					if (_instance)
						throw new Exception("StartupPlayerLoop already exists...");
					var go = new GameObject();
					_instance = go.AddComponent<StartupPlayerLoop>();
					go.name   = $"[{_instance.GetType().Name}:Manager]";
					#if UNITY_EDITOR
					EditorGUIUtility.SetIconForObject(_instance, Resources.Load<Texture2D>("Nox.CCK.Icon"));
					#endif
					_instance._resultInfos     = resultInfos;
					_instance._useOneComponent = false;
					DontDestroyOnLoad(go);
				}
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
				if (!_useOneComponent) return;
				foreach (var mod in ModManager.Mods)
					mod.Update();
			}

			private void LateUpdate() {
				if (!_useOneComponent) return;
				foreach (var mod in ModManager.Mods)
					mod.LateUpdate();
			}

			private void FixedUpdate() {
				if (!_useOneComponent) return;
				foreach (var mod in ModManager.Mods)
					mod.FixedUpdate();
			}
		}
	}
}