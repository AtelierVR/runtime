using Nox.CCK.Mods.Cores;

namespace Nox.CCK.Mods.Initializers
{
    public interface ClientModInitializer : ModInitializer
    {
        public void OnInitializeClient(ClientModCoreAPI api) { }
        public void OnUpdateClient() { }
    }
}