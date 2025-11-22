using System.Collections.Generic;
using Nox.Editor.Panel;

namespace api.nox.editor.panel {
	public class WindowManager {
		private static readonly List<Window> Windows = new();

		public static bool TryOpen(IPanel panel, Dictionary<string, object> data = null) {
			var window = Window.Create();
			if (!window) return false;

			if (!window.SetActive(panel, data))
				return false;

			Windows.Add(window);
			window.Show();
			return true;
		}

		public static void RemoveWindow(Window window)
			=> Windows.Remove(window);

		public static bool TryOpenOrFocus(IPanel panel, Dictionary<string, object> data = null) {
			foreach (var window in Windows) {
				if (window.GetActive().GetPanel() != panel) continue;
				window.Focus();
				return true;
			}

			return TryOpen(panel, data);
		}

		public static Window[] GetWindows()
			=> Windows.ToArray();
	}
}