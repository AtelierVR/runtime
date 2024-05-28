using System.Collections.Generic;
using Nox.CCK.Editor;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using UnityEngine;
using UnityEngine.UIElements;

namespace api.nox.mod
{
    public class ModEditorMod : EditorModInitializer
    {

        public void OnInitializeEditor(EditorModCoreAPI api)
        {
            Debug.Log("Hello from ModEditorMod!");
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
        public string Id { get; } = "builder";
        public string Name { get; } = "Mod/Builder";
        public bool Hidded { get; } = false;

        public VisualElement OnOpenned(Dictionary<string, object> data)
        {
            var root = new VisualElement();
            root.Add(new Label("Hello from Panel Example!"));
            return root;
        }

        public void OnClosed()
        {
            Debug.Log("Panel Example closed!");
        }
    }
}