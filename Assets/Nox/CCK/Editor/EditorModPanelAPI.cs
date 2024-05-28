namespace Nox.CCK.Editor
{
    public interface EditorModPanelAPI
    {
        public bool SetActivePanel(EditorPanel panel);
        public bool SetActivePanel(string panelId);
        public EditorPanel GetActivePanel();
        public bool IsActivePanel(EditorPanel panel);
        public bool IsActivePanel(string panelId);

        public EditorPanel GetPanel(string panelId);
        public EditorPanel[] GetPanels();
        public bool HasPanel(EditorPanel panel);
        public bool HasPanel(string panelId);

        public EditorPanel AddLocalPanel(EditorPanelBuilder panel);
        public bool RemoveLocalPanel(EditorPanel panel);
        public bool RemoveLocalPanel(string panelId);
        public bool HasLocalPanel(EditorPanel panel);
        public bool HasLocalPanel(string panelId);

        public EditorPanel GetLocalPanel(string panelId);
        public EditorPanel[] GetLocalPanels();
    }
}