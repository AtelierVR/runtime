using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Logger = Nox.CCK.Logger;

namespace api.nox.game
{
    public class GameEditorSystem : EditorModInitializer
    {
        public void OnInitializeEditor(EditorModCoreAPI api)
        {
            Logger.Log("Hello from GameEditorSystem!");
        }

        public void OnUpdateEditor()
        {
        }

        public void OnDispose()
        {
        }
    }
}