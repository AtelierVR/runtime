using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Nox.CCK.Mods.Panels
{
    public interface EditorPanelBuilder
    {
        public string GetId();
        public string GetName();
        public bool IsHidden();

        public VisualElement OnOpened(Dictionary<string, object> data);
        public void OnGUI() { }
        public void OnClosed() { }
    }
}