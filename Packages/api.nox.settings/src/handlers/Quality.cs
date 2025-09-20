using System.Linq;
using api.nox.settings.prefabs;
using Nox.CCK.Utils;
using UnityEngine;

namespace api.nox.settings.handlers {
	public sealed class Quality : DropdownHandler {
		public sealed override string[] GetPath()
			=> new[] { "graphic", "quality" };

		public static string[] GetConfigPath()
			=> new[] { "settings", "graphic", "quality" };

		public Quality() {
			SetLabel($"setting.entry.{string.Join(".", GetPath())}.label");
			SetOptions(QualitySettings.names.Select(x => (x, x)).ToArray());
			Value = Config.Load().Get(GetConfigPath(), Value);
		}

		public override void OnValueChanged(string value) {
			var index = System.Array.IndexOf(QualitySettings.names, value);
			if (index >= 0)
				Value = index;
		}

		public int Value {
			get => QualitySettings.GetQualityLevel();
			set {
				QualitySettings.SetQualityLevel(value);
				var config = Config.Load();
				config.Set(GetConfigPath(), value);
				config.Save();
			}
		}
	}
}