using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.Language {
	public class LanguageManager {
		public delegate void LanguageChanged();

		public static event LanguageChanged OnLanguageChanged;

		public delegate void PackListUpdated();

		public static event PackListUpdated OnPackListUpdated;

		public const string FALLBACK_LANGUAGE = "en-US";

		public static string DEFAULT_LANGUAGE
			=> CultureInfo.CurrentCulture.IetfLanguageTag;

		private static string _currentLanguage = DEFAULT_LANGUAGE;

		public static string CurrentLanguage {
			get => _currentLanguage;
			set {
				if (value == _currentLanguage) return;
				_currentLanguage = value;
				UpdateTexts();
				OnLanguageChanged?.Invoke();
			}
		}

		private static readonly List<LanguagePack> LanguagePacks = new();

		public static string[] GetAvailableLanguages() {
			var languages = new List<string>();
			foreach (var language in from pack in LanguagePacks
			         from language in pack.languages
			         where !languages.Contains(language.IETF)
			         select language)
				languages.Add(language.IETF);
			return languages.ToArray();
		}

		public static void UpdateTexts() {
			for (var i = 0; i < SceneManager.sceneCount; i++)
				UpdateTexts(SceneManager.GetSceneAt(i));
		}

		private static void UpdateTexts(GameObject gameObject) {
			foreach (var pack in gameObject.GetComponents<TextLanguage>())
				pack.UpdateText();
			foreach (Transform child in gameObject.transform)
				UpdateTexts(child.gameObject);
		}

		private static void UpdateTexts(Scene gameObject) {
			foreach (var root in gameObject.GetRootGameObjects())
				UpdateTexts(root);
		}

		#if UNITY_EDITOR
		[UnityEditor.MenuItem("Nox/Reload LanguageTexts")]
		public static void ReloadLanguageTexts() {
			for (var i = 0; i < SceneManager.sceneCount; i++)
				UpdateTexts(SceneManager.GetSceneAt(i));
		}

		public static string Get(string key)
			=> Get(CurrentLanguage, key);

		public static string Get(string language, string key) {
			if (Application.isPlaying)
				return GetInPacks(language, key);

			var                guids = UnityEditor.AssetDatabase.FindAssets("t:LanguagePack");
			List<LanguagePack> packs = new();
			foreach (var guid in guids)
				try {
					var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
					var pack = UnityEditor.AssetDatabase.LoadAssetAtPath<LanguagePack>(path);
					if (pack) packs.Add(pack);
				} catch {
					// ignored
				}

			var value = GetInPacks(language, key, packs);
			foreach (var pack in packs)
				Resources.UnloadAsset(pack);
			return value;
		}
		#else
        public static string Get(string key) => GetInPacks(CurrentLanguage, key);
        public static string Get(string language, string key) => GetInPacks(language, key);
		#endif

		public static string Get(string key, params object[] args) {
			var value = Get(key);
			try {
				return string.Format(value, args);
			} catch {
				// ignored
			}

			return value;
		}

		public static string Get(string language, string key, params object[] args) {
			var value = Get(language, key);
			try {
				return string.Format(value, args);
			} catch { }

			return value;
		}

		public static void AddPack(LanguagePack pack) {
			if (LanguagePacks.Contains(pack)) {
				Logger.Log($"{pack.name} updated");
				LanguagePacks.Remove(pack);
				LanguagePacks.Add(pack);
				return;
			} else {
				Logger.Log($"{pack.name} added");
				LanguagePacks.Add(pack);
			}

			OnPackListUpdated?.Invoke();
		}

		public static void RemovePack(LanguagePack pack) {
			if (!LanguagePacks.Contains(pack))
				return;

			Logger.Log($"{pack.name} removed");
			LanguagePacks.Remove(pack);

			OnPackListUpdated?.Invoke();
		}


		public static string GetInPacks(string key, List<LanguagePack> packs = null)
			=> GetInPacks(CurrentLanguage, key, packs);

		public static string GetInPacks(string language, string key, List<LanguagePack> packs = null) {
			packs ??= LanguagePacks;
			for (int i = packs.Count - 1; i >= 0; i--) {
				if (!packs[i]) continue;
				if (packs[i].TryGetLocalizedString(language, key, out string value))
					return value;
			}

			return $"[{key}]";
		}
	}
}