using System.Collections.Generic;
using Nox.CCK.Mods.Assets;
using Nox.CCK.Mods.Chat;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Groups;
using Nox.CCK.Mods.Mods;
using Nox.CCK.Mods.Networks;
using Nox.Mods.Client;

namespace Nox.Mods
{
    public class ClientModCoreAPI : CCK.Mods.Cores.ClientModCoreAPI
    {
        private RuntimeMod _mod;
        private Dictionary<string, object> _data = new();
        internal ClientModCoreAPI(RuntimeMod mod) => _mod = mod;
        public Dictionary<string, object> Data => _data;
        public CCK.Mods.ModMetadata ModMetadata => _mod.GetMetadata();

        public ChatAPI ChatAPI => throw new System.NotImplementedException();

        public GroupAPI GroupAPI => throw new System.NotImplementedException();

        public EventAPI EventAPI => throw new System.NotImplementedException();

        public NetworkAPI NetworkAPI => throw new System.NotImplementedException();

        public ModAPI ModAPI => throw new System.NotImplementedException();

        public AssetAPI AssetAPI => throw new System.NotImplementedException();
    }
}