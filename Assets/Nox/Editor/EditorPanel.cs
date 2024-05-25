using Nox.CCK.Editor;
using static Nox.CCK.Editor.EditorPanel;

namespace Nox.Editor
{
    public class EditorPanel : CCK.Editor.EditorPanel
    {
        public EditorPanel(Mods.EditorMod mod, EditorPanelBuilder panel)
        {
            _modid = mod.GetMetadata().GetId();
            _id = panel.Id;
            _name = panel.Name;
        }

        event OnPanelOpenDelegate CCK.Editor.EditorPanel.OnPanelOpen
        {
            add => OnPanelOpen += value;
            remove => OnPanelOpen -= value;
        }

        event OnPanelCloseDelegate CCK.Editor.EditorPanel.OnPanelClose
        {
            add => OnPanelClose += value;
            remove => OnPanelClose -= value;
        }

        internal void InvokeOpenPanel() => OnPanelOpen?.Invoke();
        internal void InvokeClosePanel() => OnPanelClose?.Invoke();

        private event OnPanelOpenDelegate OnPanelOpen;
        private event OnPanelCloseDelegate OnPanelClose;
        private string _modid;
        private string _id;
        private string _name;
        public string GetModId() => _modid;
        public string GetId() => _id;
        public string GetName() => _name;
        internal string GetFullId() => $"{GetModId()}.{GetId()}";
    }
}