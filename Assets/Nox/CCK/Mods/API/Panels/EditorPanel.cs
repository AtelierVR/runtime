using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Nox.CCK.Mods.Panels
{
    public interface EditorPanel
    {
        public string GetModId();
        public string GetId();
        public string GetName();
        public bool IsHidden();
        public string GetFullId() => $"{GetModId()}.{GetId()}";

        public VisualElement MakeContent(Dictionary<string, object> data = null);
        public bool IsActive();
    }
}