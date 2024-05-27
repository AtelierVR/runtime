using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using UnityEngine;

namespace api.nox.game
{
    public class GameEditorSystem : EditorModInitializer
    {

        public void OnInitializeEditor(EditorModCoreAPI api)
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