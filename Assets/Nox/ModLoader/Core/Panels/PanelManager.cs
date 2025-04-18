using System.Collections.Generic;
using Nox.CCK.Language;
using Logger = Nox.CCK.Utils.Logger;
using System;
using Nox.CCK.Utils;


#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;

namespace Nox.ModLoader.Cores.Panels
{
    public class PanelManager : EditorWindow
    {
        public static PanelManager Instance;

        [MenuItem("Nox/CCK Panel", false, 100)]
        public static void ShowWindow()
        {
            if (Instance == null)
                Instance = GetWindow<PanelManager>(LanguageManager.Get("api.nox.cck.panel.title"), true);
            else Instance.Show();
        }

        public void OnGUI()
        {
            if (!Instance) Instance = this;
            if (rootVisualElement.childCount > 0)
            {
                GetActivePanel()?.InvokeOnUpdate();
                return;
            }

            var root = Resources.Load<VisualTreeAsset>("api.nox.cck.panel").CloneTree();
            _activePanelId = null;
            rootVisualElement.Clear();
            root.style.flexGrow = 1;
            rootVisualElement.Add(root);
            UpdateMenu();

            var config = Config.LoadEditor();
            var next = config.Get("active_panel", "default");
            var panel = GetPanel(next);
            if (panel != null && !panel.IsHidden() && Goto(next) || Goto("default")) return;
            var home = new VisualElement();
            home.Add(new Label("Welcome to the Nox CCK."));
            rootVisualElement.Q<VisualElement>("content").Add(home);
        }


        public static void UpdateMenu()
        {
            if (Instance == null) return;
            var dropdown = Instance.rootVisualElement.Q<ToolbarMenu>("pages");
            dropdown.text = HasActivePanel()
                ? GetActivePanel().GetName()
                : LanguageManager.Get("api.nox.cck.panel.menu.title");
            dropdown.menu.ClearItems();
            var panels = GetPanels();
            foreach (var panel in panels)
                if (!panel.IsHidden())
                    dropdown.menu.AppendAction(panel.GetName(), a => Goto(panel.GetFullId()),
                        a => DropdownMenuAction.Status.Normal);
        }

        public static bool Goto(string id, Dictionary<string, object> data = null)
        {
            Logger.Log($"Goto panel {id}");
            var panel = GetPanel(id);
            if (panel == null) return false;
            VisualElement content = null;
            try
            {
                content = panel.MakeContent(data);
            }
            catch (Exception e)
            {
                Logger.LogError(e);

                var error = new VisualElement();
                error.Add(new Label(LanguageManager.Get("api.nox.cck.panel.error")));
                error.Add(new Label(e.Message));
                content = error;
            }

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
                    o?.InvokeOnHidden();
                }

            root.Add(content);
            content.name = id;
            Instance.ActivePanelId = id;
            panel.InvokeOnVisible();

            UpdateMenu();

            return true;
        }

        private string _activePanelId;

        public string ActivePanelId
        {
            get => _activePanelId;
            set
            {
                var config = Config.LoadEditor();
                _activePanelId = value;
                config.Set("active_panel", value);
                config.Save();
            }
        }

        public static bool HasActivePanel()
        {
            if (Instance == null) return false;
            return !string.IsNullOrEmpty(Instance.ActivePanelId) && HasPanel(Instance.ActivePanelId);
        }

        public static bool HasPanel(string panelId)
        {
            if (Instance == null) return false;
            var mods = ModManager.GetMods();
            foreach (var mod in mods)
                if (mod.CoreAPI.LocalPanelAPI.HasLocalPanel(panelId))
                    return true;
            return false;
        }

        public static Panel GetActivePanel()
        {
            if (Instance == null) return null;
            return HasActivePanel() ? GetPanel(Instance.ActivePanelId) : null;
        }

        public static bool IsActivePanel(string panelId)
        {
            if (Instance == null) return false;
            return Instance.ActivePanelId == panelId;
        }

        public static bool IsActivePanel(Panel panel)
        {
            if (Instance == null) return false;
            return IsActivePanel(panel.GetFullId());
        }

        public static Panel GetPanel(string panelId)
        {
            if (Instance == null) return null;
            var mods = ModManager.GetMods();
            foreach (var mod in mods)
                if (mod.CoreAPI.LocalPanelAPI.HasLocalPanel(panelId))
                    return mod.CoreAPI.LocalPanelAPI.GetInternalPanel(panelId);
            return null;
        }

        internal static Panel[] GetPanels()
        {
            List<Panel> panels = new();
            if (Instance == null) return panels.ToArray();
            var mods = ModManager.GetMods();
            foreach (var mod in mods)
                panels.AddRange(mod.CoreAPI.LocalPanelAPI.Panels);
            return panels.ToArray();
        }

        internal static bool SetActivePanel(Panel panel)
        {
            var fullpanel = GetPanel(panel.GetFullId());
            if (fullpanel == null) return false;
            var result = Goto(fullpanel.GetFullId());
            if (!result) return false;
            return true;
        }
    }
}

#else
namespace Nox.ModLoader.Cores.Panels
{
    public class PanelManager
    {
        public static void ShowWindow() { }
        public static void UpdateMenu() { }
        public static bool Goto(string id, Dictionary<string, object> data = null) => false;
        public static bool HasActivePanel() => false;
        public static bool HasPanel(string panelId) => false;
        public static Panel GetActivePanel() => null;
        public static bool IsActivePanel(string panelId) => false;
        public static bool IsActivePanel(Panel panel) => false;
        public static Panel GetPanel(string panelId) => null;
        internal static Panel[] GetPanels() => new Panel[0];
        internal static bool SetActivePanel(Panel panel) => false;
    }
}

#endif