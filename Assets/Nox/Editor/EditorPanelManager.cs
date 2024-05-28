#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Nox.CCK.Editor;
using Nox.CCK.Mods;
using Nox.Editor.Mods;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nox.Editor
{
    public class EditorPanelManager : EditorWindow
    {
        public static EditorPanelManager Instance;
        private void OnEnable() => Instance = this;

        [MenuItem("Nox/CCK Panel")]
        public static void ShowWindow()
        {
            if (Instance == null)
                Instance = GetWindow<EditorPanelManager>();
            else Instance.Show();
        }

        [MenuItem("Nox/Restart")]
        public static void Restart() { }

        [MenuItem("Nox/Compile")]
        public static void Compile() => AssetDatabase.Refresh();

        private void OnGUI()
        {
            if (Instance == null || rootVisualElement.childCount > 0) return;
            var root = Resources.Load<VisualTreeAsset>("api.nox.cck.panel").CloneTree();
            _activePanelId = null;
            rootVisualElement.Clear();
            root.style.flexGrow = 1;
            rootVisualElement.Add(root);
            UpdateMenu();
            if (!Goto("default"))
            {
                var home = new VisualElement();
                home.Add(new Label("Welcome to the Nox CCK."));
                rootVisualElement.Q<VisualElement>("content").Add(home);
            }
            rootVisualElement.Q<ToolbarButton>("restart").clicked += Restart;
        }

        public static void UpdateMenu()
        {
            if (Instance == null) return;
            var dropdown = Instance.rootVisualElement.Q<ToolbarMenu>("pages");
            dropdown.text = "Menu";
            dropdown.Clear();
            var panels = GetPanels();
            foreach (var panel in panels)
                if (!panel.IsHidden())
                    dropdown.menu.AppendAction(panel.GetName(), a => Goto(panel.GetFullId()), a => DropdownMenuAction.Status.Normal);
        }


        public static bool Goto(string id, Dictionary<string, object> data = null)
        {
            var panel = GetPanel(id);
            if (panel == null) return false;
            var content = panel.MakeContent(data);
            if (content == null) return false;
            var root = Instance.rootVisualElement.Q<VisualElement>("content");
            if (root == null) return false;
            content.style.flexGrow = 1;
            foreach (var child in content.Children())
                child.style.flexGrow = 1;
            foreach (var child in root.Children().ToList())
                if (child.name != id)
                {
                    root.Remove(child);
                    var o = GetPanel(child.name);
                    o?.InvokeClosePanel();
                }
            root.Add(content);
            content.name = id;
            Instance._activePanelId = id;
            panel.InvokeOpenPanel();
            return true;
        }

        private List<CCK.Editor.EditorPanel> _panels = new();
        private string _activePanelId;

        public static bool HasActivePanel()
        {
            if (Instance == null) return false;
            return !string.IsNullOrEmpty(Instance._activePanelId) && HasPanel(Instance._activePanelId);
        }

        public static bool HasPanel(string panelId)
        {
            if (Instance == null) return false;
            var mods = EditorModManager.GetMods();
            foreach (var mod in mods)
                if (mod.coreAPI.EditorPanelAPI.HasLocalPanel(panelId))
                    return true;
            return false;
        }

        public static EditorPanel GetActivePanel()
        {
            if (Instance == null) return null;
            return HasActivePanel() ? GetPanel(Instance._activePanelId) : null;
        }
        public static bool IsActivePanel(string panelId)
        {
            if (Instance == null) return false;
            return Instance._activePanelId == panelId;
        }
        public static bool IsActivePanel(EditorPanel panel)
        {
            if (Instance == null) return false;
            return IsActivePanel(panel.GetFullId());
        }

        public static EditorPanel GetPanel(string panelId)
        {
            if (Instance == null) return null;
            var mods = EditorModManager.GetMods();
            foreach (var mod in mods)
                if (mod.coreAPI.EditorPanelAPI.HasLocalPanel(panelId))
                    return mod.coreAPI.EditorPanelAPI.GetEditorPanel(panelId);
            return null;
        }

        internal static EditorPanel[] GetPanels()
        {
            List<EditorPanel> panels = new();
            if (Instance == null) return panels.ToArray();
            var mods = EditorModManager.GetMods();
            foreach (var mod in mods)
                panels.AddRange(mod.coreAPI.EditorPanelAPI._panels);
            return panels.ToArray();
        }

        internal static bool SetActivePanel(CCK.Editor.EditorPanel panel)
        {
            var fullpanel = GetPanel(panel.GetFullId());
            if (fullpanel == null) return false;
            var result = Goto(fullpanel.GetFullId());
            if (!result) return false;
            Instance._activePanelId = fullpanel.GetFullId();
            return true;
        }
    }
}
#endif