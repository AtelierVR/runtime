using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using UnityEngine;

namespace api.nox.test
{
    public class TestClientMod : ClientModInitializer
    {

        public void OnInitializeClient()
        {
        }

        public void OnInitializeClient(ModCoreAPI api)
        {
            Debug.Log("Hello from TestClientMod!");
        }

        public void OnUpdateClient()
        {

        }
        public void OnDispose()
        {

        }
    }
}