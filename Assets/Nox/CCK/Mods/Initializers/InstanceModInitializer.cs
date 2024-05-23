using Nox.CCK.Mods.Cores;

namespace Nox.CCK.Mods.Initializers {
    public interface InstanceModInitializer : BaseModInitializer  {
        public void OnInitializeInstance(ModCoreAPI api);
        public void OnUpdateInstance();
    }
}