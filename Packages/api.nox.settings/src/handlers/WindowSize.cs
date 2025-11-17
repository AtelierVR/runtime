using System.Collections.Generic;
using Nox.CCK.Settings;
using Nox.CCK.Utils;
using Nox.UI;
using Nox.UI.modals;
using UnityEngine;
namespace api.nox.settings.handlers {
	public sealed class WindowSize : DropdownHandler {
		public const string Fullscreen = "fullscreen";
		public const string Maximized  = "maximized";
		public const string Windowed   = "windowed";

		public override string[] GetPath()
			=> new[] { "graphic", "window_size" };

		private static string[] GetConfigPath()
			=> new[] { "settings", "graphic", "window_mode" };


	private static Dictionary<string, string[]> GetAvailableSize()
		=> new() {
			[Fullscreen] = new[] { "settings.entry.graphic.window_size.option.fullscreen" },
			[Maximized]  = new[] { "settings.entry.graphic.window_size.option.maximized" },
			[Windowed]   = new[] { "settings.entry.graphic.window_size.option.windowed" }
		};

		protected override GameObject GetPrefab()
			=> Main.Instance.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/dropdown.prefab");

		protected override IModalBuilder GetModalBuilder(IMenu menu)
			=> Client.UiAPI.MakeModal(menu);

		public override GameObject GetContent(RectTransform transform, IMenu menu) {
			var go = base.GetContent(transform, menu);
			SetInteractable(menu is IModalMenu);
			return go;
		}

		public WindowSize() {
			SetInteractable(false);
			SetLabel($"settings.entry.{string.Join(".", GetPath())}.label");
			SetOptions(GetAvailableSize());
			Value = Value;
			SetValue(GetCurrentWindowMode(), false);
		}

	private static string GetCurrentWindowMode() {
		if (Screen.fullScreenMode == FullScreenMode.ExclusiveFullScreen || Screen.fullScreenMode == FullScreenMode.FullScreenWindow)
			return Fullscreen;

		// Check if window is maximized
		if (Screen.fullScreenMode == FullScreenMode.MaximizedWindow || 
		    (Screen.width == Display.main.systemWidth && Screen.height == Display.main.systemHeight))
			return Maximized;

		return Windowed;
	}

	// Public method to get the current window mode from config
	public static string GetWindowMode() {
		var config = Config.Load();
		return config.Get(GetConfigPath(), GetCurrentWindowMode());
	}

	// Public method to set the window mode, with optional immediate application
	public static void SetWindowMode(string mode, bool apply = true) {
		var config = Config.Load();
		config.Set(GetConfigPath(), mode);
		config.Save();
		
		if (apply) {
			Value = mode;
		}
	}

	private static string Value {
		get => GetWindowMode();
		set {
			var currentResolution = Resolution.Value;
			
			switch (value) {
				case Fullscreen:
					// Apply fullscreen with current saved resolution
					Screen.SetResolution(currentResolution.x, currentResolution.y, FullScreenMode.ExclusiveFullScreen, Screen.currentResolution.refreshRateRatio);
					break;

				case Maximized:
					// Apply maximized mode with native resolution
					Screen.SetResolution(Display.main.systemWidth, Display.main.systemHeight, FullScreenMode.MaximizedWindow, Screen.currentResolution.refreshRateRatio);
					
					// Update resolution config to match
					var config = Config.Load();
					config.Set(Resolution.GetConfigWidthPath(), Display.main.systemWidth);
					config.Set(Resolution.GetConfigHeightPath(), Display.main.systemHeight);
					config.Save();
					break;

				case Windowed:
					// Apply windowed mode - use saved resolution if it's not native, otherwise use a reasonable windowed size
					var windowedWidth = currentResolution.x;
					var windowedHeight = currentResolution.y;
					
					if (windowedWidth == Display.main.systemWidth && windowedHeight == Display.main.systemHeight) {
						// Calculate a reasonable windowed size (80% of screen)
						windowedWidth = Mathf.RoundToInt(Display.main.systemWidth * 0.8f);
						windowedHeight = Mathf.RoundToInt(Display.main.systemHeight * 0.8f);
						
						// Update resolution config
						var cfg = Config.Load();
						cfg.Set(Resolution.GetConfigWidthPath(), windowedWidth);
						cfg.Set(Resolution.GetConfigHeightPath(), windowedHeight);
						cfg.Save();
					}
					
					Screen.SetResolution(windowedWidth, windowedHeight, FullScreenMode.Windowed, Screen.currentResolution.refreshRateRatio);
					break;
			}
			
			var config2 = Config.Load();
			config2.Set(GetConfigPath(), value);
			config2.Save();
		}
	}


	protected override void OnValueChanged(string value)
		=> Value = value;
	}
}