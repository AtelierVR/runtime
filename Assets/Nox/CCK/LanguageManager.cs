using System.Collections.Generic;
using System.Globalization;

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

        public static string Get(string key) => Get(CurrentLanguage, key);

        public static string Get(string language, string key)
        {
            for (int i = LanguagePacks.Count - 1; i >= 0; i--)
            {
                if (LanguagePacks[i] == null) continue;
                if (LanguagePacks[i].TryGetLocalizedString(language, key, out string value))
                    return value;
            }
            return $"[{key}]";
        }
    }
}