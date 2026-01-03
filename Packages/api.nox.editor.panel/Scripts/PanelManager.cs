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

		public static bool TryGetPanel(ResourceIdentifier path, out IPanel panel)
			=> path.HasNamespace()
				? TryGetPanelFromMod(Editor.CoreAPI?.ModAPI.GetMod(path.Namespace), path.SplitPath, out panel)
				: TryGetPanelEverywhere(path.SplitPath, out panel);

		private static bool TryGetPanelFromMod(IMod mod, string[] path, out IPanel panel) {
			if (mod == null) {
				Logger.LogError($"NoMod found for {string.Join("/", path)}");
				panel = null;
				return false;
			}

			foreach (var register in mod.GetInstances<IPanelRegister>())
			foreach (var p in register.GetPanels())
				if (p.GetPath().SequenceEqual(path)) {
					panel = p;
					return true;
				}

			foreach (var p in mod.GetInstances<IPanel>())
				if (p.GetPath().SequenceEqual(path)) {
					panel = p;
					return true;
				}

			Logger.LogError($"NoPanel found for {string.Join("/", path)} with mod {mod.GetMetadata().GetId()}");
			panel = null;
			return false;
		}

		private static bool TryGetPanelEverywhere(string[] path, out IPanel panel) {
			if (Editor.CoreAPI == null) {
				Logger.LogError($"No CoreAPI found for {string.Join("/", path)}");
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

			foreach (var mod in Editor.CoreAPI.ModAPI.GetMods())
			foreach (var p in mod.GetInstances<IPanel>())
				if (p.GetPath().SequenceEqual(path)) {
					panel = p;
					return true;
				}

			Logger.LogError($"NoPanel found for {string.Join("/", path)}");
			panel = null;
			return false;
		}
	}
}