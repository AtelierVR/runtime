using System.Collections.Generic;
using Nox.CCK.Mods.Chat;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Groups;
using Nox.CCK.Mods.Mods;
using Nox.CCK.Mods.Networks;

namespace Nox.Editor.Mods
{
    public class EditorModCoreAPI : CCK.Mods.Cores.EditorModCoreAPI
    {
        private EditorMod _mod;
        internal EditorModPanelAPI EditorPanelAPI;
        internal EditorNetworkAPI EditorNetworkAPI;
        internal EditorModAPI EditorModAPI;
        internal Dictionary<string, object> _data = new();
        internal EditorModCoreAPI(EditorMod mod)
        {
            _mod = mod;
            EditorPanelAPI = new EditorModPanelAPI(mod);
            EditorModAPI = new EditorModAPI(mod);
            EditorNetworkAPI = new EditorNetworkAPI(mod);
        }


        public ChatAPI ChatAPI => throw new System.NotImplementedException();

        public GroupAPI GroupAPI => throw new System.NotImplementedException();

        public EventAPI EventAPI => throw new System.NotImplementedException();

        public CCK.Mods.ModMetadata ModMetadata => _mod.GetMetadata();
        public NetworkAPI NetworkAPI => EditorNetworkAPI;
        public ModAPI ModAPI => EditorModAPI;
        public CCK.Editor.EditorModPanelAPI PanelAPI => EditorPanelAPI;
        public Dictionary<string, object> Data => _data;
    }
}