
using System;
using System.Collections.Generic;
using Nox.CCK.Mods;


namespace Nox.ModLoader.Cores.Events
{
    public class EventAPI : CCK.Mods.Events.EventAPI
    {
        private ModLoader.Mods.Mod Mod;
        private CCK.Mods.Events.EventEntryFlags _channel;

        internal EventAPI(ModLoader.Mods.Mod mod, CCK.Mods.Events.EventEntryFlags channel)
        {
            Mod = mod;
            _channel = channel;
        }

        private List<EventSubscription> Subscriptions = new();

        internal void Receive(EventContext context)
        {
            var data = new EventData()
            {
                EventName = context.EventName,
                Data = context.Data,
                InternalSource = context.Source,
                SourceChannel = context.Channel
            };
            foreach (var sub in Subscriptions)
                if (sub.EventName == null || sub.EventName == context.EventName)
                    sub.Callback(data);
        }

        internal void Emit(EventContext context)
        {
            var ncontext = new EventContext(context) { CurrentChannel = _channel, Source = Mod };
            var mod = context.Destination != null ? Mod.CoreAPI.LocalModAPI.GetInternalMod(context.Destination) : null;
            if (mod != null)
                mod.CoreAPI.LocalEventAPI.Receive(ncontext);
            else foreach (var imod in Mod.CoreAPI.LocalModAPI.GetInternalMods())
                {
                    if (context.Channel.HasFlag(CCK.Mods.Events.EventEntryFlags.Main))
                        imod.CoreAPI?.LocalEventAPI.Receive(ncontext);
                }
        }

        public void Emit(CCK.Mods.Events.EventContext context) => Emit(new EventContext(context));

        public void Emit(string eventName, params object[] data)
        {
            if (data.Length > 0 && data[^1] is CCK.Mods.Events.EventCallback callback)
                Emit(new EventContext()
                {
                    Data = data.Length > 1 ? data[..^1] : new object[0],
                    Destination = null,
                    EventName = eventName,
                    Source = Mod,
                    CurrentChannel = _channel,
                    Channel = _channel
                });
            else
                Emit(new EventContext()
                {
                    Data = data,
                    Destination = null,
                    EventName = eventName,
                    Source = Mod,
                    CurrentChannel = _channel,
                    Channel = _channel
                });
        }

        public CCK.Mods.Events.EventSubscription Subscribe(string eventName, CCK.Mods.Events.EventCallback callback)
            => Subscribe(new EventSubscription() { EventName = eventName, Callback = callback });

        public CCK.Mods.Events.EventSubscription Subscribe(CCK.Mods.Events.EventSubscription eventSub)
        {
            var runtime = new EventSubscription(eventSub);
            if (Subscriptions.Exists(sub => sub.UID == runtime.UID))
            {
                runtime.UID = 0;
                while (Subscriptions.Exists(sub => sub.UID == runtime.UID) || runtime.UID == uint.MaxValue)
                    runtime.UID++;
                if (runtime.UID == uint.MaxValue)
                    return null;
            }
            Subscriptions.Add(runtime);
            Subscriptions.Sort((a, b) => a.Weight.CompareTo(b.Weight));
            return eventSub;
        }

        public void Unsubscribe(CCK.Mods.Events.EventSubscription eventSub) => Unsubscribe(eventSub.UID);
        internal void Unsubscribe(EventSubscription eventSub) => Subscriptions.Remove(eventSub);
        public void Unsubscribe(uint uid) => Subscriptions.RemoveAll(sub => sub.UID == uid);

        public void UnsubscribeAll() => Subscriptions.Clear();
        public void UnsubscribeAll(string eventName) => Subscriptions.RemoveAll(sub => sub.EventName == eventName);
    }

    public class EventSubscription : CCK.Mods.Events.EventSubscription
    {
        internal EventSubscription() { }
        internal EventSubscription(CCK.Mods.Events.EventSubscription subscription)
        {
            UID = subscription.UID;
            EventName = subscription.EventName;
            Weight = subscription.Weight;
            Callback = subscription.Callback;
        }

        public uint UID { get; internal set; }
        public string EventName { get; internal set; }
        public uint Weight { get; internal set; }
        public CCK.Mods.Events.EventCallback Callback { get; internal set; }
    }

    public class EventContext : CCK.Mods.Events.EventContext
    {
        internal EventContext() { }
        internal EventContext(CCK.Mods.Events.EventContext context)
        {
            Data = context.Data;
            Destination = context.Destination;
            EventName = context.EventName;
            Channel = context.Channel;
        }

        public object[] Data { get; internal set; }
        public string Destination { get; internal set; }
        public string EventName { get; internal set; }
        public CCK.Mods.Events.EventEntryFlags Channel { get; internal set; }
        public ModLoader.Mods.Mod Source { get; internal set; }
        internal CCK.Mods.Events.EventEntryFlags CurrentChannel;
    }

    public class EventData : CCK.Mods.Events.EventData
    {
        public string EventName { get; internal set; }
        public object[] Data { get; internal set; }
        public CCK.Mods.Events.EventEntryFlags SourceChannel { get; internal set; }

        public ModLoader.Mods.Mod InternalSource { get; internal set; }
        public Mod Source => InternalSource;

        internal Action<object[]> _callback { get; set; }
        public void Callback(params object[] args) => _callback(args);
    }
}
