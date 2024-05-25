using Nox.CCK.Mods.Chat;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Groups;
using Nox.CCK.Mods.Networks;

namespace Nox.Editor.Mods
{
    public class EditorModCoreAPI : CCK.Mods.Cores.EditorModCoreAPI
    {
        private EditorMod _mod;
        internal EditorModPanelAPI EditorPanelAPI;
        internal EditorModCoreAPI(EditorMod mod)
        {
            _mod = mod;
            EditorPanelAPI = new EditorModPanelAPI(mod);
        }

        public CCK.Mods.ModMetadata ModMetadata => _mod.GetMetadata();

        public ChatManager ChatAPI => throw new System.NotImplementedException();

        public GroupManager GroupAPI => throw new System.NotImplementedException();

        public EventManager EventAPI => throw new System.NotImplementedException();

        public NetworkManager NetworkAPI => throw new System.NotImplementedException();

        public CCK.Mods.ModManager ModAPI => throw new System.NotImplementedException();

        public CCK.Editor.EditorModPanelAPI PanelAPI => EditorPanelAPI;
    }
}