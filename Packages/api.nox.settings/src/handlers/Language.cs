using System;
using System.Collections.Generic;
using api.nox.settings.prefabs;
using Nox.CCK.Language;

namespace api.nox.settings.handlers {
	public class Language : DropdownHandler, IDisposable {
		public sealed override string[] GetPath()
			=> new[] { "accessibility", "interface", "language" };

		public Language() {
			LanguageManager.OnPackListUpdated.AddListener(OnPacksUpdated);
			LanguageManager.OnLanguageChanged.AddListener(OnLanguageChanged);
			OnPacksUpdated();
			SetLabel($"settings.entry.{string.Join(".", GetPath())}.label");
		}

		public override void OnValueChanged(string value)
			=> LanguageManager.CurrentLanguage = value;

		private void OnPacksUpdated() {
			var packs = LanguageManager.GetAvailableLanguages();
			var res   = new List<(string, string)>();

			foreach (var pack in packs) {
				var name = LanguageManager.Get(pack, "language");
				if (string.IsNullOrWhiteSpace(name))
					name = pack;
				res.Add((pack, name));
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