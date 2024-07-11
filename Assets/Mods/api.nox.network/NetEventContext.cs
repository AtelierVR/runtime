using Nox.CCK.Mods.Events;

namespace api.nox.network
{
    public class NetEventContext : EventContext
    {
        private readonly object[] _data;
        private readonly string _eventName;
        public NetEventContext(string eventName, params object[] data)
        {
            _eventName = eventName;
            _data = data;
        }
        public object[] Data => _data;
        public string Destination => null;
        public string EventName => _eventName;
        public EventEntryFlags Channel => EventEntryFlags.Client | EventEntryFlags.Main | EventEntryFlags.Editor;
    }
}