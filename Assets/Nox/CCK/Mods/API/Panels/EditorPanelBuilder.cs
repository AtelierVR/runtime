using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Nox.CCK.Mods.Panels
{
    public interface EditorPanelBuilder
    {
        public string GetId();
        public string GetName();
        public bool IsHidden();

        public VisualElement Make(Dictionary<string, object> data);

        public void OnUpdate()
        {
        }

        public void OnHidden()
        {
        }

        public void OnVisible()
        {
        }
    }
}