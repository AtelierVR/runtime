
using System.Collections.Generic;
using Nox.CCK.Mods.Assets;
using Nox.CCK.Mods.Chat;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Groups;
using Nox.CCK.Mods.Mods;
using Nox.CCK.Mods.Networks;
using Nox.Mods.Assets;
using Nox.Mods.Client;

namespace Nox.Mods
{
    public class RuntimeModCoreAPI : CCK.Mods.Cores.ModCoreAPI
    {
        private RuntimeMod _mod;
        private RuntimeAssetAPI RuntimeAssetAPI;
        private Dictionary<string, object> _data = new();
        internal RuntimeModCoreAPI(RuntimeMod mod)
        {
            _mod = mod;
            RuntimeAssetAPI = new RuntimeAssetAPI(mod);
        }

        public Dictionary<string, object> Data => _data;
        public CCK.Mods.ModMetadata ModMetadata => _mod.GetMetadata();

        public ChatAPI ChatAPI => throw new System.NotImplementedException();
        public GroupAPI GroupAPI => throw new System.NotImplementedException();
        public EventAPI EventAPI => throw new System.NotImplementedException();
        public NetworkAPI NetworkAPI => throw new System.NotImplementedException();
        public ModAPI ModAPI => throw new System.NotImplementedException();
        public AssetAPI AssetAPI => RuntimeAssetAPI;
    }
}