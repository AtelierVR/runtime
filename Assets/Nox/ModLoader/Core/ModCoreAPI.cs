using System.Collections.Generic;
using Nox.CCK.Mods;
using Nox.CCK.Mods.Assets;
using Nox.CCK.Mods.Chats;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Groups;
using Nox.CCK.Mods.Libs;
using Nox.CCK.Mods.Metadata;
using Nox.CCK.Mods.Mods;
using Nox.CCK.Mods.Panels;
using Nox.ModLoader.Cores.Panels;

namespace Nox.ModLoader
{
    public class CoreAPI : ModCoreAPI, MainModCoreAPI, ServerModCoreAPI, ClientModCoreAPI, InstanceModCoreAPI, EditorModCoreAPI
    {
        internal Mods.Mod Mod;
        internal PanelAPI LocalPanelAPI;
        internal Cores.Mods.ModAPI LocalModAPI;
        internal Cores.Events.EventAPI LocalEventAPI;

        public CoreAPI(Mods.Mod mod)
        {
            Mod = mod;
            LocalPanelAPI = new PanelAPI(mod);
            LocalModAPI = new Cores.Mods.ModAPI(mod);
            LocalEventAPI = new Cores.Events.EventAPI(mod, EventEntryFlags.Main);
        }

        public Dictionary<string, object> Data = new();

        public ModMetadata ModMetadata => Mod.Metadata;

        public ChatAPI ChatAPI => throw new System.NotImplementedException();

        public GroupAPI GroupAPI => throw new System.NotImplementedException();

        public EditorLibsAPI LibsAPI => throw new System.NotImplementedException();


        public AssetAPI AssetAPI => Mod.AssetAPI;
        public EditorModPanelAPI PanelAPI => LocalPanelAPI;
        public ModAPI ModAPI => LocalModAPI;
        public EventAPI EventAPI => LocalEventAPI;
    }
}