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

        public bool HasActivePanel() => EditorPanelManager.HasActivePanel();
        public bool HasPanel(string panelId) => _panels.Any(p => p.GetId() == panelId || p.GetFullId() == panelId);
        public bool AddPanel(EditorPanelBuilder panel)
        {
            if (HasPanel(panel.Id))
                return false;
            _panels.Add(new EditorPanel(_mod, panel));
            return true;
        }
        public bool RemovePanel(string panelId) => HasPanel(panelId) && RemovePanel(GetPanel(panelId));
        public bool RemovePanel(CCK.Editor.EditorPanel panel)
        {
            if (!HasPanel(panel.GetId()))
                return false;
            var fullpanel = GetEditorPanel(panel.GetFullId());
            if (fullpanel.GetFullId() == EditorPanelManager.GetActivePanel().GetFullId())
            {
                fullpanel.InvokeClosePanel();
                SetActivePanel("default");
            }
            _panels.Remove(fullpanel);
            return true;
        }
        public bool SetActivePanel(CCK.Editor.EditorPanel panel) => EditorPanelManager.SetActivePanel(panel);
        public bool SetActivePanel(string panelId) => HasPanel(panelId) && SetActivePanel(GetPanel(panelId));
        public CCK.Editor.EditorPanel GetActivePanel() => HasActivePanel() ? EditorPanelManager.GetActivePanel() : null;
        public CCK.Editor.EditorPanel GetPanel(string panelId) => GetEditorPanel(panelId) ?? EditorPanelManager.GetPanel(panelId);

        public CCK.Editor.EditorPanel[] GetPanels() => EditorPanelManager.GetPanels();

        internal EditorPanel GetEditorPanel(string panelId) => _panels.FirstOrDefault(p => p.GetId() == panelId || p.GetFullId() == panelId);
    }
}