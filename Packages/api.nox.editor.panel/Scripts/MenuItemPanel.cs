using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nox.CCK.Mods.Cores;
using Nox.Editor.Panel;
using UnityEditor;

namespace api.nox.editor.panel {
	public class MenuItemPanel : IPanelRegister {
		private MenuItemMethodPanel[] _panels = Array.Empty<MenuItemMethodPanel>();

		public void OnInitializeEditor(IEditorModCoreAPI api) {
			_panels = AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(assembly => assembly.GetTypes())
				.SelectMany(type => type.GetMethods(BindingFlags.Static | BindingFlags.Public))
				.Select(method => method.GetCustomAttributes(typeof(MenuItem), false).FirstOrDefault() is not MenuItem attr ? default : (attr, method))
				.Where(pair => pair != default && pair.attr.menuItem.StartsWith("Nox/"))
				.OrderBy(pair => pair.attr.priority)
				.Select(method => new MenuItemMethodPanel(method.attr, method.method))
				.ToArray();
		}

		public void OnDisposeEditor()
			=> _panels = Array.Empty<MenuItemMethodPanel>();
		
		public IPanel[] GetPanels()
			=> _panels.Cast<IPanel>().ToArray();

		public bool TryGetPanel(string[] path, out IPanel panel)
			=> (panel = _panels.FirstOrDefault(p => p.GetPath().SequenceEqual(path))) != null;
	}

	public class MenuItemMethodPanel : IPanel {
		private readonly MethodInfo _method;
		private readonly MenuItem   _attr;

		public MenuItemMethodPanel(MenuItem attr, MethodInfo method) {
			_attr   = attr;
			_method = method;
		}

		public string[] GetPath()
			=> _attr.menuItem.Split('/');

		public IInstance[] GetInstances()
			=> Array.Empty<IInstance>();

		public string GetLabel()
			=> "Others/" + _attr.menuItem[4..];

		public IInstance Instantiate(IWindow window, Dictionary<string, object> data) {
			_method.Invoke(null, null);
			return null;
		}
	}
}