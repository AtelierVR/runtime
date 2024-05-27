using Nox.CCK.Mods.Cores;

namespace Nox.CCK.Editor
{
    public interface EditorModPanelAPI
    {
        public EditorPanel AddPanel(EditorPanelBuilder panel);

        public bool RemovePanel(EditorPanel panel);
        public bool RemovePanel(string panelId);

        public bool SetActivePanel(EditorPanel panel);
        public bool SetActivePanel(string panelId);

        public bool HasPanel(EditorPanel panel);
        public bool HasPanel(string panelId);

        public EditorPanel GetActivePanel();

        public bool IsActivePanel(EditorPanel panel);
        public bool IsActivePanel(string panelId);

        public EditorPanel GetPanel(string panelId);
        public EditorPanel[] GetPanels();
    }
}