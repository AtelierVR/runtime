using System.Collections.Generic;
using Nox.CCK.Editor;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using UnityEngine.UIElements;
using Logger = Nox.CCK.Logger;

namespace api.nox.test
{
    public class TestEditorMod : EditorModInitializer
    {

        public void OnInitializeEditor(EditorModCoreAPI api)
        {
            Logger.Log("Hello from TestEditorMod!");
            api.PanelAPI.AddLocalPanel(new PanelExample());
        }

        public void OnUpdateEditor()
        {
        }

        public void OnDispose()
        {
        }
    }

    public class PanelExample : EditorPanelBuilder
    {
        public string Id { get; } = "panel_example";
        public string Name { get; } = "Test/Example";
        public bool Hidded { get; } = false;

        public VisualElement OnOpenned(Dictionary<string, object> data)
        {
            var root = new VisualElement();
            root.Add(new Label("Hello from Panel Example!"));
            return root;
        }

        public void OnClosed()
        {
            Logger.Log("Panel Example closed!");
        }
    }
}