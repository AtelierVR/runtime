using Nox.CCK.Mods.Initializers;
using UnityEngine;

namespace api.nox.test
{
    public class TestMod : ModInitializer
    {

        public void OnInitialize()
        {
            Debug.Log("Hello from TestMod!");
        }

        public void OnUpdate()
        {
        }
        
        public void OnDispose()
        {
        }
    }
}