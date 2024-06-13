using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace api.nox.game
{
    public class GameClientSystem : ClientModInitializer
    {
        private ClientModCoreAPI coreAPI;

        public void OnInitialize(ModCoreAPI api)
        {
        }

        public void OnInitializeClient(ClientModCoreAPI api)
        {
            coreAPI = api;
            Debug.Log("Hello from GameSystem!");
            var scene = api.AssetAPI.LoadLocalWorld("default");
        }

        public void OnDispose()
        {
        }
    }
}