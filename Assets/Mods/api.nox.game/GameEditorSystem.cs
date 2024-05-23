using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using UnityEditor;
using UnityEngine;

namespace api.nox.game
{
    [InitializeOnLoad]
    public class GameEditorSystem : EditorModInitializer
    {

        public void OnInitializeEditor(ModCoreAPI api)
        {
            Debug.Log("Hello from GameEditorSystem!");
        }

        public void OnUpdateEditor()
        {
        }

        public void OnDispose()
        {
        }
    }
}