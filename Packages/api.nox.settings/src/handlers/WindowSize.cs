using System.Collections.Generic;
using Nox.CCK.Settings;
using Nox.CCK.Utils;
using Nox.UI;
using Nox.UI.modals;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.settings.handlers {
	public sealed class WindowSize : DropdownHandler {
		private const string Fullscreen = "fullscreen";
		private const string Maximized  = "maximized";
		private const string Windowed   = "windowed";

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
			if (Screen.fullScreen)
				return Fullscreen;

			// Check if window is maximized (approximate check)
			if (Screen.width == Display.main.systemWidth && Screen.height == Display.main.systemHeight)
				return Maximized;

			return Windowed;
		}

		private static string Value {
			get {
				var config = Config.Load();
				return config.Get(GetConfigPath(), Windowed);
			}
			set {
				switch (value) {
					case Fullscreen:
						Screen.fullScreen = true;
						break;

					case Maximized:
						Screen.fullScreen = false;
						Screen.SetResolution(Display.main.systemWidth, Display.main.systemHeight, FullScreenMode.MaximizedWindow);
						break;

					case Windowed:
						Screen.fullScreen = false;
						break;
				}

				var config = Config.Load();
				config.Set(GetConfigPath(), value);
				config.Save();
			}
		}


		protected override void OnValueChanged(string value)
			=> Value = value;
	}
}