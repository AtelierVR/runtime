using Nox.CCK.Editor;

namespace Nox.CCK.Mods.Cores
{
    public interface EditorModCoreAPI : ModCoreAPI
    {
        public Editor.EditorModPanelAPI PanelAPI { get; }
    }
}