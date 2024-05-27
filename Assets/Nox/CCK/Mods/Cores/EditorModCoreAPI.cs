using Nox.CCK.Editor;

namespace Nox.CCK.Mods.Cores
{
    public interface EditorModCoreAPI : ModCoreAPI
    {
        public EditorModPanelAPI PanelAPI { get; }
    }
}