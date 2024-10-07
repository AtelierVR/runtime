using System.Linq;
using api.nox.game.sessions;
using Nox.CCK;
using Nox.CCK.Mods;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Initializers;
using Nox.SimplyLibs;

namespace api.nox.game
{
    public class GameSystem : ModInitializer
    {
        private LanguagePack langpack;
        internal static GameSystem instance;
        internal SessionManager sessionManager;
        internal ModCoreAPI coreAPI;
        internal SimplyNetworkAPI NetworkAPI => coreAPI.ModAPI.GetMod("network")?.GetMainClasses().OfType<ShareObject>().FirstOrDefault()?.Convert<SimplyNetworkAPI>();

        public void OnInitialize(ModCoreAPI api)
        {
            coreAPI = api;
            instance = this;
            langpack = api.AssetAPI.GetLocalAsset<LanguagePack>("langpack");
            LanguageManager.LanguagePacks.Add(langpack);
            sessionManager = new SessionManager(this);
        }

        public void OnDispose()
        {
            LanguageManager.LanguagePacks.Remove(langpack);
            langpack = null;
            instance = null;
            sessionManager.Dispose();
            sessionManager = null;
        }

        public void OnSessionChanged(Session old, Session value)
        {
            coreAPI.EventAPI.Emit(new EventSessionChanged(old, value));
        }


    }
}

class EventSessionChanged : EventContext
{
    public EventSessionChanged(Session old, Session value)
    {
        _data = new object[] { old, value };
    }
    public object[] _data;
    public object[] Data => _data;
    public string Destination => null;
    public string EventName => "game.session.changed";
    public EventEntryFlags Channel => EventEntryFlags.All;
}