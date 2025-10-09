using System.Linq;
using Nox.CCK.Language;
using Nox.CCK.Settings;
using Nox.CCK.Utils;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.settings.handlers {
	public sealed class Resolution : DropdownHandler {
		public override string[] GetPath()
			=> new[] { "graphic", "resolution" };

		protected override GameObject GetPrefab()
			=> Main.Instance.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/dropdown.prefab");

		public static string[] GetConfigPath(string s)
			=> new[] { "settings", "graphic", s };

		public static string[] GetConfigWidthPath()
			=> GetConfigPath("width");

		public static string[] GetConfigHeightPath()
			=> GetConfigPath("height");

		Vector2Int Value {
			get {
				var config = Config.Load();
				var width  = config.Get(GetConfigWidthPath(), Screen.currentResolution.width);
				var height = config.Get(GetConfigHeightPath(), Screen.currentResolution.height);
				return new Vector2Int(width, height);
			}
			set {
				var config = Config.Load();
				Screen.SetResolution(value.x, value.y, Screen.fullScreenMode, Screen.currentResolution.refreshRateRatio);
				config.Set(GetConfigWidthPath(), value.x);
				config.Set(GetConfigHeightPath(), value.y);
				config.Save();
			}
		}

		public Vector2Int FromString(string s) {
			var parts = s.Split('x');
			if (parts.Length != 2 || !int.TryParse(parts[0], out var width) || !int.TryParse(parts[1], out var height))
				return new Vector2Int(0, 0);
			return new Vector2Int(width, height);
		}

		public string ToString(Vector2Int v)
			=> $"{v.x}x{v.y}";

		public static (string, string)[] GetAvailableResolutions()
			=> Screen.resolutions
				.Where(res => res.refreshRateRatio.Equals(Screen.currentResolution.refreshRateRatio)) // Filter by current refresh rate
				.Select(
					res => (
					LanguageManager.Get(
						"settings.entry.graphic.resolution.option",
						res.width, res.height, res.refreshRateRatio.ToString()
					),
					$"{res.width}x{res.height}"
					)
				)
				.Distinct()
				.ToArray();


		public Resolution() {
			SetLabel($"settings.entry.{string.Join(".", GetPath())}.label");

			SetOptions(GetAvailableResolutions());

			// Set current value
			var currentRes = Value;
			var currentStr = $"{currentRes.x}x{currentRes.y}";
			SetValue(currentStr);
		}

		public override void OnValueChanged(string value) {
			// Parse resolution string (e.g., "1920x1080")
			var parts = value.Split('x');
			if (parts.Length != 2 || !int.TryParse(parts[0], out var width) || !int.TryParse(parts[1], out var height)) return;
			// Find matching resolution with current refresh rate
			var targetResolution = Screen.resolutions
				.FirstOrDefault(res => res.width == width && res.height == height && res.refreshRateRatio.Equals(Screen.currentResolution.refreshRateRatio));

			if (targetResolution.width != 0 && targetResolution.height != 0) {
				Value = new Vector2Int(targetResolution.width, targetResolution.height);
				return;
			}

			Logger.LogWarning($"Resolution {value} with current refresh rate not found. Available resolutions:");
			foreach (var res in Screen.resolutions)
				Logger.LogWarning($"{res.width}x{res.height} @ {res.refreshRateRatio}");
		}
	}
}