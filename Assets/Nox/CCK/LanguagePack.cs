using System;
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
            public string IETF;

            public List<LanguageEntry> entries = new();
        }

        [System.Serializable]
        public class LanguageEntry
        {
            public string key;
            public string value;
        }

        public LanguageData[] languages;

        public string GetLocalizedString(string key, string language)
        {
            foreach (var lang in languages)
                if (lang.IETF == language)
                    foreach (var entry in lang.entries)
                        if (entry.key == key)
                            return entry.value;
            return null;
        }

        internal bool TryGetLocalizedString(string language, string key, out string value)
        {
            value = GetLocalizedString(key, language);
            return value != null;
        }
    }
}