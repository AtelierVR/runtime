namespace Nox.CCK.Mods.Initializers
{
    public interface ModInitializer
    {
        public void OnInitialize();
        public void OnUpdate();
        public void OnDispose();
    }
}