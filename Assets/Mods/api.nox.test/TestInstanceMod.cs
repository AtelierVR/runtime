using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Logger = Nox.CCK.Logger;

namespace api.nox.test
{
    public class TestInstanceMod : InstanceModInitializer
    {

        public void OnInitializeInstance(InstanceModCoreAPI api)
        {
            Logger.Log("Hello from TestInstanceMod!");
        }

        public void OnUpdateInstance()
        {
        }

        public void OnDispose()
        {
        }
    }
}