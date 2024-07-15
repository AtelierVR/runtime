using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nox.CCK
{
    public class LanguageManager
    {
        public delegate void LanguageChanged();
        public static event LanguageChanged OnLanguageChanged;

        public const string FALLBACK_LANGUAGE = "en-US";
        public static string DEFAULT_LANGUAGE => CultureInfo.CurrentCulture.IetfLanguageTag;

        private static string _currentLanguage = DEFAULT_LANGUAGE;
        public static string CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (value == _currentLanguage) return;
                _currentLanguage = value;
                OnLanguageChanged?.Invoke();
            }
        }

        public static List<LanguagePack> LanguagePacks = new();

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Nox/Reload LanguageTexts")]
        public static void ReloadLanguageTexts()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
                foreach (var root in SceneManager.GetSceneAt(i).GetRootGameObjects())
                    UpdateTexts(root);
        }

        public static void UpdateTexts(GameObject gameObject)
        {
            foreach (var pack in gameObject.GetComponents<TextLanguage>())
                pack.UpdateText();
            foreach (Transform child in gameObject.transform)
                UpdateTexts(child.gameObject);
        }

        public static string Get(string key) => Get(CurrentLanguage, key);
        public static string Get(string language, string key)
        {
            if (Application.isPlaying)
                return GetInPacks(language, key);

            var guids = UnityEditor.AssetDatabase.FindAssets("t:LanguagePack");
            List<LanguagePack> packs = new();
            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var pack = UnityEditor.AssetDatabase.LoadAssetAtPath<LanguagePack>(path);
                if (pack != null) packs.Add(pack);
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



        public static string GetInPacks(string key, List<LanguagePack> packs = null) => GetInPacks(CurrentLanguage, key, packs);

        public static string GetInPacks(string language, string key, List<LanguagePack> packs = null)
        {
            packs ??= LanguagePacks;
            for (int i = packs.Count - 1; i >= 0; i--)
            {
                if (packs[i] == null) continue;
                if (packs[i].TryGetLocalizedString(language, key, out string value))
                    return value;
            }
            return $"[{key}]";
        }
    }
}