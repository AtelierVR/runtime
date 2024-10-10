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
        internal static GameSystem Instance;
        internal SessionManager SessionManager;
        internal ModCoreAPI CoreAPI;
        internal SimplyNetworkAPI NetworkAPI => CoreAPI.ModAPI.GetMod("network")?.GetMainClasses().OfType<ShareObject>().FirstOrDefault()?.Convert<SimplyNetworkAPI>();

        public void OnInitialize(ModCoreAPI api)
        {
            CoreAPI = api;
            Instance = this;
            langpack = api.AssetAPI.GetLocalAsset<LanguagePack>("langpack");
            LanguageManager.LanguagePacks.Add(langpack);
            SessionManager = new SessionManager(this);
        }

        public void OnDispose()
        {
            LanguageManager.LanguagePacks.Remove(langpack);
            langpack = null;
            Instance = null;
            SessionManager.Dispose();
            SessionManager = null;
        }

        public void OnSessionChanged(Session old, Session value)
        {
            CoreAPI.EventAPI.Emit(new EventSessionChanged(old, value));
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