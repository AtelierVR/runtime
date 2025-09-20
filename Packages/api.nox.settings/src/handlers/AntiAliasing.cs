using api.nox.settings.prefabs;
using Nox.CCK.Utils;
using UnityEngine;

namespace api.nox.settings.handlers {
	public sealed class AntiAliasing : DropdownHandler {
		public override string[] GetPath()
			=> new[] { "graphic", "anti_aliasing" };

		public static string[] GetConfigPath()
			=> new[] { "settings", "graphic", "msaa" };

		private static readonly (string, string)[] AntiAliasingOptions = {
			("Off", 0.ToString()),
			("2x", 2.ToString()),
			("4x", 4.ToString()),
			("8x", 8.ToString())
		};

		public AntiAliasing() {
			SetLabel($"settings.entry.{string.Join(".", GetPath())}.label");
			SetOptions(AntiAliasingOptions);
			Value = Config.Load().Get(GetConfigPath(), Value);
			SetValue(Value.ToString(), false);
		}

		public override void OnValueChanged(string value) {
			if (int.TryParse(value, out var msaaLevel))
				Value = msaaLevel;
		}

		public int Value {
			get => QualitySettings.antiAliasing;
			set {
				QualitySettings.antiAliasing = value;
				var config = Config.Load();
				config.Set(GetConfigPath(), value);
				config.Save();
			}
		}
	}
}