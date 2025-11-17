using System;
using System.Collections.Generic;
using Nox.CCK.Language;
using Nox.CCK.Settings;
using Nox.CCK.Utils;
using Nox.UI;
using Nox.UI.modals;
using UnityEngine;

namespace api.nox.settings.handlers {
	public sealed class Language : DropdownHandler, IDisposable {
		public override string[] GetPath()
			=> new[] { "accessibility", "interface", "language" };

		protected override GameObject GetPrefab()
			=> Main.Instance.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/dropdown.prefab");

		protected override IModalBuilder GetModalBuilder(IMenu menu)
			=> Client.UiAPI.MakeModal(menu);

		public Language() {
			LanguageManager.OnPackListUpdated.AddListener(OnPacksUpdated);
			LanguageManager.OnLanguageChanged.AddListener(OnLanguageChanged);
			OnPacksUpdated();
			SetLabel($"settings.entry.{string.Join(".", GetPath())}.label");
			Value = Config.Load().Get("settings.language", Value);
			SetValue(LanguageManager.CurrentLanguage, false);
		}


		protected override void OnValueChanged(string value)
			=> Value = value;

		private static string Value {
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
			var res   = new Dictionary<string, string[]>();

			foreach (var lang in langs) {
				var name = LanguageManager.Get(lang, "language");
				if (string.IsNullOrEmpty(name))
					name = lang;
				res.Add(lang, new[] { "value", name });
			}

			SetOptions(res);
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