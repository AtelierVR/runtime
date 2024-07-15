using System.Collections.Generic;
using Nox.CCK.Mods.Assets;
using Nox.CCK.Mods.Chat;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Groups;
using Nox.CCK.Mods.Mods;
using Nox.CCK.Mods.XR;

namespace Nox.Editor.Mods
{
    public class EditorModCoreAPI : CCK.Mods.Cores.EditorModCoreAPI
    {
        private EditorMod _mod;
        internal EditorModPanelAPI EditorPanelAPI;
        internal EditorModAPI EditorModAPI;
        internal EditorLibsAPI EditorLibsAPI;
        internal EditorAssetAPI EditorAssetAPI;
        internal Dictionary<string, object> _data = new();
        internal EditorModCoreAPI(EditorMod mod)
        {
            _mod = mod;
            EditorPanelAPI = new EditorModPanelAPI(mod);
            EditorModAPI = new EditorModAPI(mod);
            EditorLibsAPI = new EditorLibsAPI(mod);
            EditorAssetAPI = new EditorAssetAPI(mod);
        }

        public CCK.Mods.ModMetadata ModMetadata => _mod.GetMetadata();
        public Dictionary<string, object> Data => _data;
        // public NetworkAPI NetworkAPI => EditorNetworkAPI;
        public ModAPI ModAPI => EditorModAPI;

        public ChatAPI ChatAPI => throw new System.NotImplementedException();
        public GroupAPI GroupAPI => throw new System.NotImplementedException();
        public EventAPI EventAPI => throw new System.NotImplementedException();
        public AssetAPI AssetAPI => EditorAssetAPI;
        public CCK.Editor.EditorModPanelAPI PanelAPI => EditorPanelAPI;
        public CCK.Editor.EditorLibsAPI LibsAPI => EditorLibsAPI;
        public XRAPI XRAPI => throw new System.NotImplementedException();
    }
}