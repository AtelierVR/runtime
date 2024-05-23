using Nox.CCK.Mods.Initializers;

namespace Nox.CCK.Mods
{
    public interface Mod
    {
        public ModMetadata GetMetadata();
        public T GetAssembly<T>() where T : ModInitializer;
    }
}