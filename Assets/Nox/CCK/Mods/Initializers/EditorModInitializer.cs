using Nox.CCK.Mods.Cores;

namespace Nox.CCK.Mods.Initializers
{
    public interface EditorModInitializer : ModInitializer
    {
        public void OnInitializeEditor(EditorModCoreAPI api) { }
        public void OnUpdateEditor() { }
    }
}