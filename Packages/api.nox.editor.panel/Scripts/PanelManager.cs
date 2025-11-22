using System;
using System.Linq;
using Nox.CCK.Mods;
using Nox.CCK.Utils;
using Nox.Editor.Panel;

namespace api.nox.editor.panel {
	public class PanelManager {
		private static IPanel[] GetPanelsByRegister()
			=> Editor.CoreAPI?.ModAPI
					.GetMods()
					.SelectMany(mod => mod.GetInstances<IPanelRegister>())
					.SelectMany(register => register.GetPanels())
					.ToArray()
				?? Array.Empty<IPanel>();

		private static IPanel[] GetDirectPanels()
			=> Editor.CoreAPI?.ModAPI
					.GetMods()
					.SelectMany(mod => mod.GetInstances<IPanel>())
					.ToArray()
				?? Array.Empty<IPanel>();

		public static IPanel[] GetPanels()
			=> GetPanelsByRegister()
				.Concat(GetDirectPanels())
				.ToArray();

		public static bool TryGetPanel(ResourceIdentifier id, out IPanel panel)
			=> id.HasGroup()
				? TryGetPanelFromMod(Editor.CoreAPI?.ModAPI.GetMod(id.GetGroup()), id.GetPath(), out panel)
				: TryGetPanelEverywhere(id.GetPath(), out panel);

		private static bool TryGetPanelFromMod(IMod mod, string[] path, out IPanel panel) {
			if (mod == null) {
				panel = null;
				return false;
			}

			foreach (var register in mod.GetInstances<IPanelRegister>())
			foreach (var p in register.GetPanels())
				if (p.GetPath().SequenceEqual(path)) {
					panel = p;
					return true;
				}

			panel = null;
			return false;
		}

		private static bool TryGetPanelEverywhere(string[] path, out IPanel panel) {
			if (Editor.CoreAPI == null) {
				panel = null;
				return false;
			}

			foreach (var mod in Editor.CoreAPI.ModAPI.GetMods())
			foreach (var register in mod.GetInstances<IPanelRegister>())
			foreach (var p in register.GetPanels())
				if (p.GetPath().SequenceEqual(path)) {
					panel = p;
					return true;
				}

			panel = null;
			return false;
		}
	}
}