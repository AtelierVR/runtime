using System;
using System.Collections.Generic;
using System.Linq;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using Nox.Editor.Panel;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using IPanel = Nox.Editor.Panel.IPanel;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.editor.panel {
	public class DefaultPanel : IEditorModInitializer, IPanel {
		private static readonly string[] PanelPath = { "default" };

		[MenuItem("Nox/Panel", priority = 1)]
		public static void OpenDefaultPanel() {
			var panel = PanelManager.TryGetPanel(new ResourceIdentifier(null, PanelPath), out var p) ? p : null;
			if (panel == null) {
				Logger.LogError("DefaultPanel not found in PanelManager.");
				foreach (var pa in PanelManager.GetPanels())
					Logger.Log($"Registered panel: {string.Join("/", pa.GetPath())}");
				return;
			}

			if (WindowManager.TryOpenOrFocus(panel, new Dictionary<string, object>()))
				return;

			Logger.LogError("Failed to open DefaultPanel.");
		}

		public string[] GetPath()
			=> PanelPath;

		internal DefaultInstance Instance;

		public IInstance[] GetInstances()
			=> Instance != null
				? new IInstance[] { Instance }
				: Array.Empty<IInstance>();

		public string GetLabel()
			=> "Home";

		public IInstance Instantiate(IWindow window, Dictionary<string, object> data) {
			if (Instance != null)
				throw new InvalidOperationException("DefaultPanel only supports a single instance.");
			return Instance = new DefaultInstance(this, window, data);
		}
	}

	public class DefaultInstance : IInstance {
		private readonly DefaultPanel _panel;
		private readonly IWindow      _window;

		public DefaultInstance(DefaultPanel panel, IWindow window, Dictionary<string, object> data) {
			_panel  = panel;
			_window = window;
		}

		public IPanel GetPanel()
			=> _panel;

		public IWindow GetWindow()
			=> _window;

		public string GetTitle()
			=> _panel.GetLabel();

		public void OnDestroy()
			=> _panel.Instance = null;

		public VisualElement GetContent() {
			var root = new VisualElement();
			root.Add(
				new Label("Welcome to the Default Panel!") {
					style = {
						unityFontStyleAndWeight = FontStyle.Bold,
						fontSize                = 24,
						marginTop               = 20,
						marginBottom            = 20,
						alignSelf               = Align.Center
					}
				}
			);
			return root;
		}
	}
}