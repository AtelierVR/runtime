using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using UnityEditor;
using UnityEngine;

namespace api.nox.test
{
    [InitializeOnLoad]
    public class TestEditorMod : EditorModInitializer
    {

        public void OnInitializeEditor(ModCoreAPI api)
        {
            Debug.Log("Hello from TestEditorMod!");
        }

        public void OnUpdateEditor()
        {
        }

        public void OnDispose()
        {
        }
    }
}