using System.Collections.Generic;
using Nox.CCK.Language;
using Nox.CCK.Settings;
using Nox.CCK.Utils;
using Nox.UI;
using Nox.UI.modals;
using UnityEngine;

namespace api.nox.settings.handlers {
	public sealed class AntiAliasing : DropdownHandler {
		public override string[] GetPath()
			=> new[] { "graphic", "anti_aliasing" };

		protected override GameObject GetPrefab()
			=> Main.Instance.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/dropdown.prefab");

		protected override IModalBuilder GetModalBuilder(IMenu menu)
			=> Client.UiAPI.MakeModal(menu);

		private static string[] GetConfigPath()
			=> new[] { "settings", "graphic", "msaa" };

		private static Dictionary<string, string[]> GetAntiAliasingOptions()
			=> new() {
				[0.ToString()] = new[] { "settings.entry.graphic.anti_aliasing.option.off" },
				[1.ToString()] = new[] { "settings.entry.graphic.anti_aliasing.option.x2" },
				[2.ToString()] = new[] { "settings.entry.graphic.anti_aliasing.option.x4" },
				[3.ToString()] = new[] { "settings.entry.graphic.anti_aliasing.option.x8" }
			};

		public AntiAliasing() {
			SetLabel($"settings.entry.{string.Join(".", GetPath())}.label");
			SetOptions(GetAntiAliasingOptions());
			Value = Config.Load().Get(GetConfigPath(), Value);
			SetValue(Value.ToString(), false);
		}

		protected override void OnValueChanged(string value) {
			if (!int.TryParse(value, out var msaaLevel)) return;
			Value = msaaLevel;
		}

		private static int Value {
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