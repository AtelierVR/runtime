using Nox.CCK.Mods.Cores;

namespace Nox.CCK.Editor
{
    public interface EditorModPanelAPI
    {
        public bool AddPanel(EditorPanelBuilder panel);
        public bool RemovePanel(EditorPanel panel);
        public bool SetActivePanel(EditorPanel panel);
        public bool SetActivePanel(string panelId);
        public bool HasPanel(string panelId);
        public EditorPanel GetActivePanel();
        public EditorPanel GetPanel(string panelId);
        public EditorPanel[] GetPanels();
    }
}