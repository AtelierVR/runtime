
using System;
using System.Collections.Generic;
using Nox.CCK.Mods;
using Nox.CCK.Mods.Events;
using Nox.Mods.Client;

namespace Nox.Mods.Assets
{
    public class RuntimeEventAPI : EventAPI
    {
        private RuntimeMod _mod;
        private EventEntryFlags _channel;

        internal RuntimeEventAPI(RuntimeMod mod, EventEntryFlags channel)
        {
            _mod = mod;
            _channel = channel;
        }

        private List<RuntimeEventSubscription> _subscriptions = new();
        internal void Receive(RuntimeEventContext context)
        {
            Debug.Log($"Receiving event {context.EventName} in {_mod.GetMetadata().GetId()} at {_channel}");
            var data = new RuntimeEventData()
            {
                EventName = context.EventName,
                Data = context.Data,
                Source = context.Source,
                SourceChannel = context.Channel
            };
            foreach (var sub in _subscriptions)
                if (sub.EventName == null || sub.EventName == context.EventName)
                    sub.Callback(data);
        }

        public void Emit(EventContext context)
        {
            Debug.Log($"Emitting event {context.EventName} in {_mod.GetMetadata().GetId()} at {_channel}");
            var ncontext = new RuntimeEventContext(context) { CurrentChannel = _channel, Source = _mod };
            var mod = context.Destination != null ? _mod.coreAPI.RuntimeModAPI.GetInternalMod(context.Destination) : null;
            if (mod != null)
                mod.coreAPI.RuntimeEventAPI.Receive(ncontext);
            else foreach (var imod in _mod.coreAPI.RuntimeModAPI.GetInternalMods())
                {
                    if (context.Channel.HasFlag(EventEntryFlags.Main))
                        imod.coreAPI?.RuntimeEventAPI.Receive(ncontext);
                    if (context.Channel.HasFlag(EventEntryFlags.Client))
                        imod.coreClientAPI?.RuntimeEventAPI.Receive(ncontext);
                }
        }

        public void Emit(string eventName, params object[] data)
        {

            if (data.Length > 0 && data[data.Length - 1] is EventCallback callback)
                Emit(new RuntimeEventContext()
                {
                    Data = data.Length > 1 ? data[..^1] : new object[0],
                    Destination = null,
                    EventName = eventName,
                    Source = _mod,
                    CurrentChannel = _channel,
                    Channel = _channel
                });
            else
                Emit(new RuntimeEventContext()
                {
                    Data = data,
                    Destination = null,
                    EventName = eventName,
                    Source = _mod,
                    CurrentChannel = _channel,
                    Channel = _channel
                });
        }

        public EventSubscription Subscribe(string eventName, EventCallback callback) => Subscribe(new RuntimeEventSubscription()
        {
            EventName = eventName,
            Callback = callback
        });

        public EventSubscription Subscribe(EventSubscription eventSub)
        {
            var runtime = new RuntimeEventSubscription(eventSub);
            if (_subscriptions.Exists(sub => sub.UID == runtime.UID))
            {
                runtime.UID = 0;
                while (_subscriptions.Exists(sub => sub.UID == runtime.UID) || runtime.UID == uint.MaxValue)
                    runtime.UID++;
                if (runtime.UID == uint.MaxValue)
                    return null;
            }
            _subscriptions.Add(runtime);
            _subscriptions.Sort((a, b) => a.Weight.CompareTo(b.Weight));
            Debug.Log($"Subscribing to event {runtime.EventName} in {_mod.GetMetadata().GetId()} at {_channel}");
            return eventSub;
        }

        public void Unsubscribe(EventSubscription eventSub) => _subscriptions.Remove((RuntimeEventSubscription)eventSub);
        public void Unsubscribe(uint uid) => _subscriptions.RemoveAll(sub => sub.UID == uid);
        public void UnsubscribeAll() => _subscriptions.Clear();
        public void UnsubscribeAll(string eventName) => _subscriptions.RemoveAll(sub => sub.EventName == eventName);
    }

    public class RuntimeEventSubscription : EventSubscription
    {
        internal RuntimeEventSubscription() { }
        internal RuntimeEventSubscription(EventSubscription subscription)
        {
            UID = subscription.UID;
            EventName = subscription.EventName;
            Weight = subscription.Weight;
            Callback = subscription.Callback;
        }

        public uint UID { get; internal set; }
        public string EventName { get; internal set; }
        public uint Weight { get; internal set; }
        public EventCallback Callback { get; internal set; }
    }

    public class RuntimeEventContext : EventContext
    {
        internal RuntimeEventContext() { }
        internal RuntimeEventContext(EventContext context)
        {
            Data = context.Data;
            Destination = context.Destination;
            EventName = context.EventName;
            Channel = context.Channel;
        }
        public object[] Data { get; internal set; }
        public string Destination { get; internal set; }
        public string EventName { get; internal set; }
        public EventEntryFlags Channel { get; internal set; }
        public RuntimeMod Source { get; internal set; }
        internal EventEntryFlags CurrentChannel;
    }

    public class RuntimeEventData : EventData
    {
        public string EventName { get; internal set; }
        public object[] Data { get; internal set; }
        public Mod Source { get; internal set; }
        public EventEntryFlags SourceChannel { get; internal set; }
        internal Action<object[]> _callback { get; set; }
        public void Callback(params object[] args) => _callback(args);
    }
}
