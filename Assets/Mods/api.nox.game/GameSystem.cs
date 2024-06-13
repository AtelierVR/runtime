using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using UnityEngine;

namespace api.nox.game {
    public class GameSystem : ClientModInitializer
    {
        private ClientModCoreAPI coreAPI;

        public void OnInitialize(ClientModCoreAPI api)
        {
            coreAPI = api;

            Debug.Log("Hello from GameSystem!");
        }

        public void OnDispose()
        {
        }
    }
}