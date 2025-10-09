using Nox.CCK.Language;
using Nox.CCK.Settings;
using Nox.CCK.Utils;
using UnityEngine;

namespace api.nox.settings.handlers {
	public sealed class AntiAliasing : DropdownHandler {
		public override string[] GetPath()
			=> new[] { "graphic", "anti_aliasing" };

		protected override GameObject GetPrefab()
			=> Main.Instance.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/dropdown.prefab");


		public static string[] GetConfigPath()
			=> new[] { "settings", "graphic", "msaa" };

		private static (string, string)[] GetAntiAliasingOptions()
			=> new[] {
				(LanguageManager.Get("settings.entry.graphic.anti_aliasing.option.off"), 0.ToString()),
				(LanguageManager.Get("settings.entry.graphic.anti_aliasing.option.x2"), 1.ToString()),
				(LanguageManager.Get("settings.entry.graphic.anti_aliasing.option.x4"), 2.ToString()),
				(LanguageManager.Get("settings.entry.graphic.anti_aliasing.option.x8"), 3.ToString())
			};

		public AntiAliasing() {
			SetLabel($"settings.entry.{string.Join(".", GetPath())}.label");
			SetOptions(GetAntiAliasingOptions());
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