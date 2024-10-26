using System.Linq;
using api.nox.game.sessions;
using api.nox.network;
using api.nox.xr;
using Cysharp.Threading.Tasks;
using Nox.CCK;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Initializers;
using UnityEngine;
using Logger = Nox.CCK.Logger;

namespace api.nox.game
{
    public class GameSystem : ModInitializer
    {
        private LanguagePack langpack;
        internal static GameSystem Instance;
        internal ModCoreAPI CoreAPI;
        internal NetworkSystem NetworkAPI => CoreAPI.ModAPI.GetMod("network")?.GetMainClasses().OfType<NetworkSystem>().FirstOrDefault();
        internal XRSystem XRAPI => CoreAPI.ModAPI.GetMod("xr")?.GetMainClasses().OfType<XRSystem>().FirstOrDefault();



        public void OnInitialize(ModCoreAPI api)
        {
            Logger.Log("GameSystem initialized");
            CoreAPI = api;
            Instance = this;
            langpack = api.AssetAPI.GetLocalAsset<LanguagePack>("langpack");
            LanguageManager.LanguagePacks.Add(langpack);
        }

        public void OnDispose()
        {
            LanguageManager.LanguagePacks.Remove(langpack);
            langpack = null;
            Instance = null;
        }
    }
}
