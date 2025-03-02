using System.Collections.Generic;
using Nox.CCK.Mods.Panels;
using UnityEngine.UIElements;

namespace Nox.ModLoader.Cores.Panels
{
    public class Panel : EditorPanel
    {
        internal string _modid;

        private readonly EditorPanelBuilder Builder;

        public Panel(EditorPanelBuilder panel)
        {
            Builder = panel;
        }
        
        internal void InvokeOpenPanel() => Builder?.OnOpenned(null);
        internal void InvokeClosePanel() => Builder?.OnClosed();
        internal void InvokePanelGUI() => Builder?.OnGUI();

        public string GetModId() => _modid;
        public string GetId() => Builder.Id;
        public string GetName() => Builder.Name;
        public bool IsHidden() => Builder.Hidded;
        internal string GetFullId() => $"{GetModId()}.{GetId()}";

        public VisualElement MakeContent(Dictionary<string, object> data = null) => Builder.OnOpenned(data);
        public bool IsActive() => PanelManager.IsActivePanel(this);
    }
}