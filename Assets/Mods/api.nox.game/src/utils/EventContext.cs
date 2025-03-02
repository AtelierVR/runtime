using Nox.CCK.Mods.Events;
using CCKEventContext = Nox.CCK.Mods.Events.EventContext;

namespace api.nox.game.utils
{
    public class EventContext : CCKEventContext
    {
        public EventContext(string destination, string eventName, EventEntryFlags channel, params object[] data)
        {
            Destination = destination;
            EventName = eventName;
            Channel = channel;
            Data = data;
        }
        
        public object[] Data { get; }
        public string Destination { get; }
        public string EventName { get; }
        public EventEntryFlags Channel { get; }
    }
}