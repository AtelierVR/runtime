using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using UnityEngine;

namespace api.nox.test
{
    public class TestInstanceMod : InstanceModInitializer
    {

        public void OnInitializeInstance(InstanceModCoreAPI api)
        {
            Debug.Log("Hello from TestInstanceMod!");
        }

        public void OnUpdateInstance()
        {
        }

        public void OnDispose()
        {
        }
    }
}