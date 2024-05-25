namespace Nox.CCK.Editor
{
    public interface EditorPanel
    {
        public string GetModId();
        public string GetId();
        public string GetName();
        public string GetFullId() => $"{GetModId()}.{GetId()}";

        public delegate void OnPanelOpenDelegate();
        public delegate void OnPanelCloseDelegate();
        public event OnPanelOpenDelegate OnPanelOpen;
        public event OnPanelCloseDelegate OnPanelClose;
    }
}