using System.Collections.Generic;
using System.Linq;
using Nox.CCK.Settings;
using Nox.CCK.Utils;
using Nox.UI;
using Nox.UI.modals;
using UnityEngine;

namespace api.nox.settings.handlers {
	public sealed class Resolution : DropdownHandler {
		public override string[] GetPath()
			=> new[] { "graphic", "resolution" };

		public static string[] GetConfigPath(string s)
			=> new[] { "settings", "graphic", s };

		public static string[] GetConfigWidthPath()
			=> GetConfigPath("width");

		public static string[] GetConfigHeightPath()
			=> GetConfigPath("height");

		protected override GameObject GetPrefab()
			=> Main.Instance.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/dropdown.prefab");

		protected override IModalBuilder GetModalBuilder(IMenu menu)
			=> Client.UiAPI.MakeModal(menu);

		public Resolution() {
			SetInteractable(false);
			SetLabel($"settings.entry.{string.Join(".", GetPath())}.label");
			SetOptions(GetAvailableResolutions());
			var v = Value;
			Value = v;
			SetButtonText("settings.entry.graphic.resolution.option", v.x.ToString(), v.y.ToString());
		}

		public static Vector2Int Value {
			get {
				var config = Config.Load();
				var width  = config.Get(GetConfigWidthPath(), Screen.currentResolution.width);
				var height = config.Get(GetConfigHeightPath(), Screen.currentResolution.height);
				return new Vector2Int(width, height);
			}
			set {
				var config = Config.Load();
				var screen = Screen.currentResolution;
				var mode   = Screen.fullScreenMode;
				if (value.x == screen.width && value.y == screen.height)
					mode = FullScreenMode.MaximizedWindow;
				Screen.SetResolution(value.x, value.y, mode, Screen.currentResolution.refreshRateRatio);
				config.Set(GetConfigWidthPath(), value.x);
				config.Set(GetConfigHeightPath(), value.y);
				config.Save();
			}
		}

		private static Vector2Int FromString(string s) {
			var parts = s.Split('x');
			if (parts.Length != 2 || !int.TryParse(parts[0], out var width) || !int.TryParse(parts[1], out var height))
				return Vector2Int.zero;
			return new Vector2Int(width, height);
		}

		private static string ToString(Vector2Int v)
			=> $"{v.x}x{v.y}";

		private static Dictionary<string, string[]> GetAvailableResolutions()
			=> Screen.resolutions
				.Select(r => new Vector2Int(r.width, r.height))
				.Distinct()
				.OrderBy(r => r.x * r.y)
				.Reverse()
				.Select(
					res => (ToString(res), new[] {
						"settings.entry.graphic.resolution.option",
						res.x.ToString(),
						res.y.ToString()
					})
				)
				.ToDictionary(t => t.Item1, t => t.Item2);


		protected override void OnValueChanged(string value) {
			var res = FromString(value);
			Value = new Vector2Int(res.x, res.y);
			SetButtonText("settings.entry.graphic.resolution.option", res.x.ToString(), res.y.ToString());
		}
	}
}