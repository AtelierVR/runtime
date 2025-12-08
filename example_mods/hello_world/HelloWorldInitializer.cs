using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using UnityEngine;

namespace HelloWorldMod
{
    /// <summary>
    /// Example mod initializer that prints "Hello World!" during initialization.
    /// </summary>
    public class HelloWorldInitializer : IMainModInitializer
    {
        private IModCoreAPI _api;

        /// <summary>
        /// Called when the mod is being initialized.
        /// </summary>
        /// <param name="api">The core API for the mod.</param>
        public void OnInitialize(IModCoreAPI api)
        {
            _api = api;
            Debug.Log("[HelloWorld] OnInitialize called!");
        }

        /// <summary>
        /// Called when the mod is being initialized in the main application.
        /// This is where we print our Hello World message!
        /// </summary>
        /// <param name="api">The main core API.</param>
        public void OnInitializeMain(MainModCoreAPI api)
        {
            Debug.Log("===========================================");
            Debug.Log("          Hello World!");
            Debug.Log("   From HelloWorldMod example mod");
            Debug.Log("===========================================");
            
            // You can also use the logger API if available
            _api?.LoggerAPI?.Log("Hello World from LoggerAPI!");
        }

        /// <summary>
        /// Called after the mod has been initialized in the main application.
        /// </summary>
        public void OnPostInitializeMain()
        {
            Debug.Log("[HelloWorld] OnPostInitializeMain - Mod fully initialized!");
        }

        /// <summary>
        /// Called before the mod is disposed.
        /// </summary>
        public void OnPreDispose()
        {
            Debug.Log("[HelloWorld] OnPreDispose - Mod is about to be disposed!");
        }

        /// <summary>
        /// Called when the mod is being disposed.
        /// </summary>
        public void OnDispose()
        {
            Debug.Log("[HelloWorld] OnDispose - Goodbye World!");
            _api = null;
        }
    }
}
