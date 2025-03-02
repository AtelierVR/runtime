using System.Collections.Generic;
using System.Linq;
using Nox.CCK.Mods.Panels;

namespace Nox.ModLoader.Cores.Panels
{
    public class PanelAPI : EditorModPanelAPI
    {
        public ModLoader.Mods.Mod Mod;
        internal PanelAPI(ModLoader.Mods.Mod mod) { Mod = mod; }

        internal List<Panel> Panels = new();

        // set panel active
        public bool SetActivePanel(EditorPanel panel) => panel != null && SetActivePanel(panel.GetFullId());
        public bool SetActivePanel(string panelId) => PanelManager.HasPanel(panelId) && PanelManager.SetActivePanel(PanelManager.GetPanel(panelId));

        // check if panel is active
        public EditorPanel GetActivePanel() => PanelManager.GetActivePanel();
        public bool IsActivePanel(EditorPanel panel) => PanelManager.IsActivePanel(panel.GetFullId());
        public bool IsActivePanel(string panelId) => PanelManager.IsActivePanel(panelId);

        // get panel
        public EditorPanel GetPanel(string panelId) => PanelManager.GetPanel(panelId);
        public EditorPanel[] GetPanels() => PanelManager.GetPanels();
        public EditorPanel GetLocalPanel(string panelId) => GetInternalPanel(panelId);
        public EditorPanel[] GetLocalPanels() => Panels.ToArray();
        internal Panel GetInternalPanel(string panelId) => Panels.FirstOrDefault(panel => panel.GetId() == panelId || panel.GetFullId() == panelId);

        // has panel
        public bool HasPanel(EditorPanel panel) => PanelManager.HasPanel(panel.GetFullId());
        public bool HasPanel(string panelId) => PanelManager.HasPanel(panelId);
        public bool HasLocalPanel(EditorPanel panel) => HasLocalPanel(panel.GetFullId());
        public bool HasLocalPanel(string panelId) => Panels.Any(panel => panel.GetId() == panelId || panel.GetFullId() == panelId);

        // add panel
        public EditorPanel AddLocalPanel(EditorPanelBuilder panel)
        {
            if (HasLocalPanel(panel.Id)) return null;
            var editorpanel = new Panel(panel) { _modid = Mod.Metadata.GetId() };
            Panels.Add(editorpanel);
            PanelManager.UpdateMenu();
            return editorpanel;
        }
        public bool RemoveLocalPanel(EditorPanel panel)
        {
            if (!HasLocalPanel(panel)) return false;
            var fullpanel = GetInternalPanel(panel.GetFullId());
            Panels.Remove(fullpanel);
            PanelManager.UpdateMenu();
            return true;
        }

        /// <summary>
        /// Remove a local panel by its ID
        /// </summary>
        /// <param name="panelId"></param>
        /// <returns></returns>
        public bool RemoveLocalPanel(string panelId) => HasLocalPanel(panelId) && RemoveLocalPanel(GetLocalPanel(panelId));

        /// <summary>
        /// Update the panel list
        /// </summary>
        public void UpdatePanelList() => PanelManager.UpdateMenu();
    }
}