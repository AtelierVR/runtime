using System.Collections.Generic;
using Nox.CCK.Mods.Panels;
using UnityEngine.UIElements;

namespace Nox.ModLoader.Cores.Panels
{
    public class Panel : EditorPanel
    {
        internal string ModId;

        private readonly EditorPanelBuilder _builder;

        public Panel(EditorPanelBuilder panel) 
            => _builder = panel;
        
        internal void InvokeOpenPanel() => _builder?.OnOpened(null);
        internal void InvokeClosePanel() => _builder?.OnClosed();
        internal void InvokePanelGUI() => _builder?.OnGUI();

        public string GetModId() => ModId;
        public string GetId() => _builder.GetId();
        public string GetName() => _builder.GetName();
        public bool IsHidden() => _builder.IsHidden();
        public string GetFullId() => $"{GetModId()}.{GetId()}";

        public VisualElement MakeContent(Dictionary<string, object> data = null) => _builder.OnOpened(data);
        public bool IsActive() => PanelManager.IsActivePanel(this);
    }
}