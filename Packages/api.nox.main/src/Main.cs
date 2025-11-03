using System;
using Nox.CCK.Language;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Initializers;
using UnityEngine.SceneManagement;
using Logger = Nox.CCK.Utils.Logger;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace api.nox.main {
	public class Main : IMainModInitializer {
		private LanguagePack _lang;
		private IModCoreAPI  _coreAPI;

		private EventSubscription[] _events = Array.Empty<EventSubscription>();

		public void OnInitialize(IModCoreAPI api) {
			_coreAPI = api;

			api.LoggerAPI.Log("api.nox.main initialized");

			_lang = api.AssetAPI.GetAsset<LanguagePack>("pack.asset");
			LanguageManager.AddPack(_lang);

			_events = new[] {
				api.EventAPI.Subscribe("exit", OnExitEvent)
			};

			var count = SceneManager.sceneCountInBuildSettings;
			for (var i = 0; i < count; i++) {
				var path = SceneUtility.GetScenePathByBuildIndex(i);
				api.LoggerAPI.LogDebug($"Scene {i}: {path}");
			}
		}


		private void OnExitEvent(EventData data) {
			#if UNITY_EDITOR
			if (!EditorApplication.isPlaying) return;
			_coreAPI.LoggerAPI.Log("Stopping play mode due to exit event");
			EditorApplication.isPlaying = false;
			#else
			_coreAPI.LoggerAPI.Log("Quitting application due to exit event");
			UnityEngine.Application.Quit();
			#endif
		}

		public void OnDispose() {
			LanguageManager.RemovePack(_lang);
			foreach (var subscription in _events)
				_coreAPI.EventAPI.Unsubscribe(subscription);
			_events  = Array.Empty<EventSubscription>();
			_coreAPI = null;
		}
	}
}