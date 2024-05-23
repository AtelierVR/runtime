using Nox.CCK.Mods.Cores;

namespace Nox.CCK.Mods.Initializers {
    public interface EditorModInitializer : BaseModInitializer  {
        public void OnInitializeEditor(ModCoreAPI api);
        public void OnUpdateEditor();
    }
}