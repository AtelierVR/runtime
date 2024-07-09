using System;
using System.Diagnostics.Tracing;

namespace Nox.CCK.Mods.Events
{
    public interface EventAPI
    {
        public void Emit(string eventName, params object[] data);
        public void Emit(EventContext context);

        public EventSubscription Subscribe(string eventName, EventCallback callback);
        public EventSubscription Subscribe(EventSubscription eventSub);
        public void Unsubscribe(EventSubscription eventSub);
        public void Unsubscribe(uint uid);
        public void UnsubscribeAll();
        public void UnsubscribeAll(string eventName);
    }

    public delegate void EventCallback(EventData context);

    public interface EventSubscription
    {
        public uint UID { get; }
        public string EventName { get; }
        public uint Weight { get; }
        public EventCallback Callback { get; }
    }

    public interface EventData
    {
        public string EventName { get; }
        public object[] Data { get; }
        public Mod Source { get; }
        public void Callback(params object[] args);
        public EventEntryFlags SourceChannel { get; }
    }

    public interface EventContext
    {
        public object[] Data { get; }
        public string Destination { get; }
        public string EventName { get; }
        public EventEntryFlags Channel { get; }
    }

    [Flags]
    public enum EventEntryFlags
    {
        Main = 1,
        Client = 2,
        Instance = 4,
        Editor = 8,
        All = Main | Client | Instance | Editor
    }
}