using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Nox.CCK.Mods.Panels
{
    public interface EditorPanelBuilder
    {
        public string Id { get; }
        public string Name { get; }
        public bool Hidded { get; }

        public VisualElement OnOpenned(Dictionary<string, object> data);
        public void OnGUI() { }
        public void OnClosed() { }
    }
}