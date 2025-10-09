using Nox.CCK.Language;
using Nox.CCK.Settings;
using Nox.CCK.Utils;
using UnityEngine;

namespace api.nox.settings.handlers {
	public sealed class WindowSize : DropdownHandler {
		public const string Fullscreen = "fullscreen";
		public const string Maximized  = "maximized";
		public const string Windowed   = "windowed";


		public override string[] GetPath()
			=> new[] { "graphic", "window_size" };

		protected override GameObject GetPrefab()
			=> Main.Instance.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/dropdown.prefab");

		public static string[] GetConfigPath()
			=> new[] { "settings", "graphic", "window_mode" };

		private static readonly (string, string)[] WindowModeOptions = {
			(LanguageManager.Get("settings.entry.graphic.window_size.option.fullscreen"), Fullscreen),
			(LanguageManager.Get("settings.entry.graphic.window_size.option.maximized"), Maximized),
			(LanguageManager.Get("settings.entry.graphic.window_size.option.windowed"), Windowed)
		};

		public WindowSize() {
			SetLabel($"settings.entry.{string.Join(".", GetPath())}.label");
			SetOptions(WindowModeOptions);

			// Determine current window mode
			string currentMode = GetCurrentWindowMode();
			string savedMode   = Config.Load().Get(GetConfigPath(), currentMode);
			SetValue(savedMode, false);
		}

		private string GetCurrentWindowMode() {
			if (Screen.fullScreen)
				return Fullscreen;

			// Check if window is maximized (approximate check)
			if (Screen.width == Display.main.systemWidth && Screen.height == Display.main.systemHeight)
				return Maximized;

			return Windowed;
		}

		public override void OnValueChanged(string value) {
			switch (value) {
				case Fullscreen:
					Screen.fullScreen = true;
					break;

				case Maximized:
					Screen.fullScreen = false;
					// Set to maximum available resolution for windowed mode
					Screen.SetResolution(Display.main.systemWidth, Display.main.systemHeight, false);
					break;

				case Windowed:
					Screen.fullScreen = false;
					// Set to a reasonable windowed size (80% of screen)
					int windowWidth  = Mathf.RoundToInt(Display.main.systemWidth  * 0.8f);
					int windowHeight = Mathf.RoundToInt(Display.main.systemHeight * 0.8f);
					Screen.SetResolution(windowWidth, windowHeight, false);
					break;
			}

			// Save to config
			var config = Config.Load();
			config.Set(GetConfigPath(), value);
			config.Save();
		}
	}
}