using System;
using System.Collections.Generic;
using System.Linq;
using Nox.CCK.Utils;
using Nox.Editor.Panel;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;
using IPanel = Nox.Editor.Panel.IPanel;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.editor.panel {
	public class Window : UnityEditor.EditorWindow, IWindow {
		[SerializeField]
		private IInstance _active;

		[SerializeField]
		private ResourceIdentifier panelId;

		private Dictionary<string, object> _panelData;

		public IInstance GetActive()
			=> _active ??= panelId != null && PanelManager.TryGetPanel(panelId, out var panel)
				? panel.Instantiate(this, _panelData)
				: throw new InvalidOperationException($"No panel found for id '{panelId?.ToString() ?? "null"}'");

		public static Window Create() {
			var window = CreateInstance<Window>();
			window.maxSize = new Vector2(512, window.maxSize.y);
			return window;
		}

		public bool SetActive(IPanel panel, Dictionary<string, object> data = null) {
			try {
				var old = _active;
				_active = panel.Instantiate(this, data ?? new Dictionary<string, object>());
				old?.OnDestroy();
				panelId    = new ResourceIdentifier(null, panel.GetPath());
				_panelData = data ?? new Dictionary<string, object>();
				UpdateMenu();
				UpdateContent();
			} catch (Exception e) {
				Editor.CoreAPI.LoggerAPI.LogException(new Exception($"Failed to instantiate panel '{panel.GetPath()}'", e));
				return false;
			}

			return true;
		}

		public void OnDestroy() {
			Logger.LogDebug("Closing window", tag: nameof(Window), context: this);
			GetActive()?.OnDestroy();
			WindowManager.RemoveWindow(this);
			_active = null;
		}

		public new void Repaint() {
			base.Repaint();
			UpdateMenu();
			UpdateContent();
		}

		public void OnFocus() {
			GetActive().OnFocus();
			UpdateMenu();
			UpdateContent();
		}

		public new void Show() {
			Logger.LogDebug("Showing window", tag: nameof(Window), context: this);
			base.Show();
			GetActive().OnFocus();
		}

		private ToolbarMenu        _menu;
		private ToolbarBreadcrumbs _breadcrumbs;
		private VisualElement      _content;

		public ToolbarMenu Menu
			=> _menu ??= rootVisualElement.Q<ToolbarMenu>("menu");

		public ToolbarBreadcrumbs Breadcrumbs
			=> _breadcrumbs ??= rootVisualElement.Q<ToolbarBreadcrumbs>("navigation");

		public VisualElement Content
			=> _content ??= rootVisualElement.Q<VisualElement>("content");

		private void UpdateMenu() {
			if (Menu != null) {
				Menu.menu.MenuItems().Clear();
				var panels = PanelManager.GetPanels();
				foreach (var panel in panels)
					Menu.menu.AppendAction(panel.GetLabel(), OnMenuClick);
				titleContent = new GUIContent($"{GetActive().GetTitle()} - {Application.productName}");
			}

			if (Breadcrumbs != null) {
				while (Breadcrumbs.childCount > 0)
					Breadcrumbs.PopItem();
				foreach (var item in GetActive().GetPanel().GetLabel().Split('/'))
					Breadcrumbs.PushItem(item);
			}

			// other UI updates can go here
		}

		private void UpdateContent() {
			if (Content == null)
				return;
			Content.Clear();
			var content = GetActive().GetContent();
			content.style.flexGrow = 1;
			Content.Add(content);
		}

		private void OnMenuClick(DropdownMenuAction action) {
			var panels = PanelManager.GetPanels();
			foreach (var panel in panels) {
				if (panel.GetLabel() != action.name)
					continue;

				if (panel == GetActive().GetPanel()) {
					Logger.LogDebug($"Panel '{action.name}' is already active. Focusing window.", tag: nameof(Window), context: this);
					Focus();
					return;
				}

				var instances = panel.GetInstances();
				if (!panel.AllowMultiple() && instances.Length > 0) {
					Logger.LogDebug($"Panel '{action.name}' does not allow multiple instances and one is already open. Focusing existing instance.", tag: nameof(Window), context: this);
					instances[0].GetWindow().Focus();
					return;
				}

				if (!SetActive(panel)) {
					Logger.LogError($"Failed to set active panel to '{action.name}'", tag: nameof(Window), context: this);
					return;
				}

				Repaint();
				return;
			}
		}

		public void CreateGUI() {
			rootVisualElement.Clear();
			rootVisualElement.style.flexGrow = 1;
			var content = Resources.Load<VisualTreeAsset>("Document").CloneTree();
			content.styleSheets.Add(Resources.Load<StyleSheet>("Style"));
			content.styleSheets.Add(Resources.Load<StyleSheet>("styles/index"));
			content.style.flexGrow = 1;
			rootVisualElement.Add(content);
			UpdateMenu();
			UpdateContent();
		}
	}
}