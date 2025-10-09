using System;
using System.Collections.Generic;
using Nox.CCK.Language;
using Nox.CCK.Settings;
using Nox.CCK.Utils;
using UnityEngine;

namespace api.nox.settings.handlers {
	public sealed class Language : DropdownHandler, IDisposable {
		public sealed override string[] GetPath()
			=> new[] { "accessibility", "interface", "language" };

		protected override GameObject GetPrefab()
			=> Main.Instance.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/dropdown.prefab");

		public Language() {
			LanguageManager.OnPackListUpdated.AddListener(OnPacksUpdated);
			LanguageManager.OnLanguageChanged.AddListener(OnLanguageChanged);
			OnPacksUpdated();
			SetLabel($"settings.entry.{string.Join(".", GetPath())}.label");
			Value = Config.Load().Get("settings.language", Value);
			SetValue(LanguageManager.CurrentLanguage, false);
		}

		public override void OnValueChanged(string value) {
			Value = value;
		}

		private string Value {
			get => LanguageManager.CurrentLanguage;
			set {
				LanguageManager.CurrentLanguage = value;
				var config = Config.Load();
				config.Set("settings.language", value);
				config.Save();
			}
		}

		private void OnPacksUpdated() {
			var langs = LanguageManager.GetAvailableLanguages();
			var res   = new List<(string, string)>();

			foreach (var lang in langs) {
				var name = LanguageManager.Get(lang, "language");
				if (string.IsNullOrEmpty(name))
					name = lang;
				res.Add((name, lang));
			}

			SetOptions(res.ToArray());
			SetValue(LanguageManager.CurrentLanguage, false);
		}

		private void OnLanguageChanged(string lang)
			=> SetValue(lang, false);


		public void Dispose() {
			LanguageManager.OnPackListUpdated.RemoveListener(OnPacksUpdated);
			LanguageManager.OnLanguageChanged.RemoveListener(OnLanguageChanged);
		}
	}
}