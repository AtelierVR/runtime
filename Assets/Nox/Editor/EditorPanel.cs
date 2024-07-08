using System;
using System.Collections.Generic;
using Nox.CCK.Editor;
using Nox.Editor.Mods;
using UnityEngine.UIElements;
using static Nox.CCK.Editor.EditorPanel;

namespace Nox.Editor
{
    public class EditorPanel : CCK.Editor.EditorPanel
    {
        public EditorPanel(EditorMod mod, EditorPanelBuilder panel)
        {
            _modid = mod.GetMetadata().GetId();
            _panel = panel;
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
        private EditorPanelBuilder _panel;
        private EditorPanelManager _manager;
        public string GetModId() => _modid;
        public string GetId() => _panel.Id;
        public string GetName() => _panel.Name;
        public bool IsHidden() => _panel.Hidded;
        internal string GetFullId() => $"{GetModId()}.{GetId()}";

        public VisualElement MakeContent(Dictionary<string, object> data = null) => _panel.OnOpenned(data);
        public bool IsActive() => EditorPanelManager.IsActivePanel(this);
    }
}