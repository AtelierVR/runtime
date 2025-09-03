using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nox.CCK.Language {
	[CreateAssetMenu(fileName = "LanguagePack", menuName = "Nox/Language Pack", order = 1)]
	public class LanguagePack : ScriptableObject {
		[System.Serializable]
		public class LanguageData {
			public string IETF;

			public List<LanguageEntry> entries = new();
		}

		[System.Serializable]
		public class LanguageEntry {
			public string key;
			public string value;
		}

		public LanguageData[] languages;

		public string GetLocalizedString(string key, string language)
			=> (from lang in languages
				where lang.IETF == language
				from entry in lang.entries.Where(entry => entry.key == key)
				select entry.value).FirstOrDefault();


		internal bool TryGetLocalizedString(string language, string key, out string value) {
			value = GetLocalizedString(key, language);
			return value != null;
		}
		
		public bool HasLocalizationString(string language, string key)
			=> GetLocalizedString(key, language) != null;
	}
}