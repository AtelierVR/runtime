using System;
using UnityEngine;
using Nox.CCK.Language;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Initializers;
using Logger = Nox.CCK.Utils.Logger;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace api.nox.main {
	public class Main : MainModInitializer {
		private LanguagePack _lang;
		private ModCoreAPI   _coreAPI;

		private EventSubscription[] _events = Array.Empty<EventSubscription>();

		public void OnInitialize(ModCoreAPI api) {
			_coreAPI = api;

			_lang = api.AssetAPI.GetAsset<LanguagePack>("pack.asset");
			LanguageManager.AddPack(_lang);

			_events = new[] {
				api.EventAPI.Subscribe("exit", OnExitEvent)
			};
		}


		private void OnExitEvent(EventData data) {
			#if UNITY_EDITOR
			if (!EditorApplication.isPlaying) return;
			Logger.Log("Stopping play mode due to exit event");
			EditorApplication.isPlaying = false;
			#else
			Logger.Log("Quitting application due to exit event");
			Application.Quit();
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