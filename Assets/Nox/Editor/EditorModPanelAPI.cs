using System.Collections.Generic;
using System.Linq;
using Nox.CCK.Editor;

namespace Nox.Editor
{
    public class EditorModPanelAPI : CCK.Editor.EditorModPanelAPI
    {
        private Mods.EditorMod _mod;

        internal EditorModPanelAPI(Mods.EditorMod mod) => _mod = mod;

        internal List<EditorPanel> _panels = new List<EditorPanel>();

        public bool SetActivePanel(CCK.Editor.EditorPanel panel) => EditorPanelManager.SetActivePanel(panel);
        public bool SetActivePanel(string panelId) => EditorPanelManager.HasPanel(panelId) && EditorPanelManager.SetActivePanel(EditorPanelManager.GetPanel(panelId));
        public CCK.Editor.EditorPanel GetActivePanel() => EditorPanelManager.GetActivePanel();
        public bool IsActivePanel(CCK.Editor.EditorPanel panel) => EditorPanelManager.IsActivePanel(panel.GetFullId());
        public bool IsActivePanel(string panelId) => EditorPanelManager.IsActivePanel(panelId);
        public CCK.Editor.EditorPanel GetPanel(string panelId) => EditorPanelManager.GetPanel(panelId);
        public CCK.Editor.EditorPanel[] GetPanels() => EditorPanelManager.GetPanels();
        public bool HasPanel(CCK.Editor.EditorPanel panel) => EditorPanelManager.HasPanel(panel.GetFullId());
        public bool HasPanel(string panelId) => EditorPanelManager.HasPanel(panelId);
        public CCK.Editor.EditorPanel AddLocalPanel(EditorPanelBuilder panel)
        {
            if (HasLocalPanel(panel.Id)) return null;
            var editorpanel = new EditorPanel(_mod, panel);
            _panels.Add(editorpanel);
            return editorpanel;
        }
        public bool RemoveLocalPanel(CCK.Editor.EditorPanel panel)
        {
            if (!HasLocalPanel(panel)) return false;
            var fullpanel = GetEditorPanel(panel.GetFullId());
            _panels.Remove(fullpanel);
            return true;
        }
        public bool RemoveLocalPanel(string panelId) => HasLocalPanel(panelId) && RemoveLocalPanel(GetLocalPanel(panelId));
        public bool HasLocalPanel(CCK.Editor.EditorPanel panel) => HasLocalPanel(panel.GetFullId());
        public bool HasLocalPanel(string panelId) => _panels.Any(panel => panel.GetId() == panelId || panel.GetFullId() == panelId);
        public CCK.Editor.EditorPanel GetLocalPanel(string panelId) => GetEditorPanel(panelId);
        public CCK.Editor.EditorPanel[] GetLocalPanels() => _panels.ToArray();
        internal EditorPanel GetEditorPanel(string panelId) => _panels.FirstOrDefault(panel => panel.GetId() == panelId || panel.GetFullId() == panelId);

        public void UpdatePanelList() => EditorPanelManager.UpdateMenu();
    }
}