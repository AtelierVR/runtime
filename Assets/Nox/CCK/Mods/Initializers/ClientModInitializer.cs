using Nox.CCK.Mods.Cores;

namespace Nox.CCK.Mods.Initializers
{
    public interface ClientModInitializer : BaseModInitializer
    {
        public void OnInitializeClient(ModCoreAPI api);
        public void OnUpdateClient();
    }
}