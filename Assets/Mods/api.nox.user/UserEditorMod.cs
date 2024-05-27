#if UNITY_EDITOR
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;

namespace api.nox.world
{
    public class UserEditorMod : EditorModInitializer
    {
        internal EditorModCoreAPI _api;

        public void OnInitializeEditor(EditorModCoreAPI api)
        {
            _api = api;
        }

        public void OnDispose()
        {

        }

        public void OnUpdateEditor()
        {
        }
    }
}
#endif