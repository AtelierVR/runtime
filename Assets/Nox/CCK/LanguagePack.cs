using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Nox.CCK
{

    [CreateAssetMenu(fileName = "LanguagePack", menuName = "Nox/Language Pack", order = 1)]
    public class LanguagePack : ScriptableObject
    {
        [System.Serializable]
        public class LanguageData
        {
            public string iso;

            public List<LanguageEntry> entries = new();
        }

        [System.Serializable]
        public class LanguageEntry
        {
            public string key;
            public string value;
        }

        public LanguageData[] languages;

        public string GetLocalizedString(string key, string language, params object[] args)
        {
            foreach (var lang in languages)
                if (lang.iso == language)
                    foreach (var entry in lang.entries)
                        if (entry.key == key)
                            return string.Format(entry.value, args);
            return string.Format("[{0}:{1}]", language, key);
        }

        public static string CurrentISO => CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
    }
}