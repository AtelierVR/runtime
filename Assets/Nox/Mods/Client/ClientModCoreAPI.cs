using System.Collections.Generic;
using System.Linq;
using Nox.CCK.Mods.Assets;
using Nox.CCK.Mods.Chat;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Groups;
using Nox.CCK.Mods.Mods;
using Nox.CCK.Mods.Networks;
using Nox.CCK.Mods.XR;
using Nox.Mods.Assets;
using Nox.Mods.Client;
using Nox.Mods.Mods;

namespace Nox.Mods
{
    public class ClientModCoreAPI : CCK.Mods.Cores.ClientModCoreAPI
    {
        internal RuntimeMod _mod;
        internal RuntimeAssetAPI RuntimeAssetAPI;
        internal RuntimeModAPI RuntimeModAPI;
        internal RuntimeEventAPI RuntimeEventAPI;
        internal Dictionary<string, object> _data = new();
        internal ClientModCoreAPI(RuntimeMod mod)
        {
            _mod = mod;
            RuntimeAssetAPI = new RuntimeAssetAPI(mod);
            RuntimeModAPI = new RuntimeModAPI(mod);
            RuntimeEventAPI = new RuntimeEventAPI(mod, EventEntryFlags.Client);
        }
        public Dictionary<string, object> Data => _data;
        public CCK.Mods.ModMetadata ModMetadata => _mod.GetMetadata();

        public ChatAPI ChatAPI => throw new System.NotImplementedException();
        public GroupAPI GroupAPI => throw new System.NotImplementedException();
        public EventAPI EventAPI => RuntimeEventAPI;
        public ModAPI ModAPI => RuntimeModAPI;
        public AssetAPI AssetAPI => RuntimeAssetAPI;
        public NetworkAPI NetworkAPI => ModAPI.GetMod("network")?.GetMainClasses().OfType<NetworkAPI>().First();
        public XRAPI XRAPI => ModAPI.GetMod("xr")?.GetMainClasses().OfType<XRAPI>().First();
    }
}