using System;
using System.Collections.Generic;
using Nox.CCK.Mods;
using Nox.CCK.Mods.Events;

namespace Nox.Editor.Mods.Events
{
    public class EditorEventAPI : EventAPI
    {
        private readonly EditorMod _mod;
        private readonly EventEntryFlags _channel;
        internal EditorEventAPI(EditorMod mod, EventEntryFlags channel)
        {
            _mod = mod;
            _channel = channel;
        }

        private List<EditorEventSubscription> _subscriptions = new();
        internal void Receive(EditorEventContext context)
        {
            var data = new EditorEventData()
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
            var ncontext = new EditorEventContext(context) { CurrentChannel = _channel, Source = _mod };
            var mod = context.Destination != null ? _mod.coreAPI.EditorModAPI.GetEditorMod(context.Destination) : null;
            if (mod != null)
                mod.coreAPI.EditorEventAPI.Receive(ncontext);
            else foreach (var imod in _mod.coreAPI.EditorModAPI.GetEditorMods())
                {
                    if (context.Channel.HasFlag(EventEntryFlags.Main))
                        imod.coreAPI?.EditorEventAPI.Receive(ncontext);
                    if (context.Channel.HasFlag(EventEntryFlags.Editor))
                        imod.coreAPI?.EditorEventAPI.Receive(ncontext);
                }
        }

        public void Emit(string eventName, params object[] data)
        {
            if (data.Length > 0 && data[data.Length - 1] is EventCallback callback)
                Emit(new EditorEventContext()
                {
                    Data = data.Length > 1 ? data[..^1] : new object[0],
                    Destination = null,
                    EventName = eventName,
                    Source = _mod,
                    CurrentChannel = _channel,
                    Channel = _channel
                });
            else
                Emit(new EditorEventContext()
                {
                    Data = data,
                    Destination = null,
                    EventName = eventName,
                    Source = _mod,
                    CurrentChannel = _channel,
                    Channel = _channel
                });
        }

        public EventSubscription Subscribe(string eventName, EventCallback callback) => Subscribe(new EditorEventSubscription()
        {
            EventName = eventName,
            Callback = callback
        });

        public EventSubscription Subscribe(EventSubscription eventSub)
        {
            var editor = new EditorEventSubscription(eventSub);
            if (_subscriptions.Exists(sub => sub.UID == editor.UID))
            {
                editor.UID = 0;
                while (_subscriptions.Exists(sub => sub.UID == editor.UID) || editor.UID == uint.MaxValue)
                    editor.UID++;
                if (editor.UID == uint.MaxValue)
                    return null;
            }
            _subscriptions.Add(editor);
            _subscriptions.Sort((a, b) => a.Weight.CompareTo(b.Weight));
            Debug.Log($"Subscribing to event {editor.EventName} in {_mod.GetMetadata().GetId()} at {_channel}");
            return eventSub;
        }

        public void Unsubscribe(EventSubscription eventSub) => _subscriptions.Remove((EditorEventSubscription)eventSub);
        public void Unsubscribe(uint uid) => _subscriptions.RemoveAll(sub => sub.UID == uid);
        public void UnsubscribeAll() => _subscriptions.Clear();
        public void UnsubscribeAll(string eventName) => _subscriptions.RemoveAll(sub => sub.EventName == eventName);
    }
    public class EditorEventSubscription : EventSubscription
    {
        internal EditorEventSubscription() { }
        internal EditorEventSubscription(EventSubscription subscription)
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

    public class EditorEventContext : EventContext
    {
        internal EditorEventContext() { }
        internal EditorEventContext(EventContext context)
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
        public EditorMod Source { get; internal set; }
        internal EventEntryFlags CurrentChannel;
    }

    public class EditorEventData : EventData
    {
        public string EventName { get; internal set; }
        public object[] Data { get; internal set; }
        public Mod Source { get; internal set; }
        public EventEntryFlags SourceChannel { get; internal set; }
        internal Action<object[]> _callback { get; set; }
        public void Callback(params object[] args) => _callback(args);
    }
}