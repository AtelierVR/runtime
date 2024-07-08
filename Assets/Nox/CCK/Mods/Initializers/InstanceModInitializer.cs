using Nox.CCK.Mods.Cores;

namespace Nox.CCK.Mods.Initializers {
    public interface InstanceModInitializer : ModInitializer  {
        public void OnInitializeInstance(InstanceModCoreAPI api) {}
        public void OnUpdateInstance() {}
    }
}