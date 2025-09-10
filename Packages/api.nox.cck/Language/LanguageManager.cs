using System;
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

		public const string FallbackLanguage = "en-US";

		public static string DefaultLanguage
			=> CultureInfo.CurrentCulture.IetfLanguageTag;

		private static string _currentLanguage = DefaultLanguage;

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

		public static string Get(string key)
			=> Get(CurrentLanguage, key) ?? Get(FallbackLanguage, key) ?? $"[{key}]";

		#if UNITY_EDITOR
		[UnityEditor.MenuItem("Nox/Reload LanguageTexts")]
		public static void ReloadLanguageTexts() {
			for (var i = 0; i < SceneManager.sceneCount; i++)
				UpdateTexts(SceneManager.GetSceneAt(i));
		}

		public static string Get(string language, string key) {
			if (Application.isPlaying)
				return GetInPacks(language, key);

			var guids = UnityEditor.AssetDatabase.FindAssets("t:LanguagePack");

			List<LanguagePack> packs = new();
			foreach (var guid in guids)
				try {
					var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
					var pack = UnityEditor.AssetDatabase.LoadAssetAtPath<LanguagePack>(path);
					if (pack) packs.Add(pack);
				} catch (Exception e) {
					Logger.LogException(e);
				}

			var value = GetInPacks(language, key, packs);
			foreach (var pack in packs)
				Resources.UnloadAsset(pack);
			return value;
		}
		#else
        public static string Get(string language, string key) 
			=> GetInPacks(language, key);
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
			} catch (Exception e) {
				Logger.LogException(e);
			}

			return value;
		}

		public static void AddPack(LanguagePack pack) {
			LanguagePacks.Add(pack);
			OnPackListUpdated?.Invoke();
		}

		public static void RemovePack(LanguagePack pack) {
			if (!LanguagePacks.Contains(pack)) return;
			LanguagePacks.Remove(pack);
			OnPackListUpdated?.Invoke();
		}


		public static string GetInPacks(string key, List<LanguagePack> packs = null)
			=> GetInPacks(CurrentLanguage, key, packs);

		public static string GetInPacks(string language, string key, List<LanguagePack> packs = null) {
			packs ??= LanguagePacks;
			foreach (var t in packs.Where(t => t)) 
				if (t.TryGetLocalizedString(language, key, out var value))
					return value;
			return null;
		}

		public static bool Has(string language, string key, List<LanguagePack> packs = null) {
			packs ??= LanguagePacks;
			return packs.Where(t => t).Any(t => t.HasLocalizationString(language, key));
		}

		public static bool Has(string key, List<LanguagePack> packs = null)
			=> Has(CurrentLanguage, key, packs);
	}
}