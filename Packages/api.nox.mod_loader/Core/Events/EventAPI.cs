using System;
using System.Collections.Generic;
using Nox.CCK.Mods;
using Nox.CCK.Utils;


namespace Nox.ModLoader.Cores.Events {
	public class EventAPI : CCK.Mods.Events.EventAPI {
		private ModLoader.Mods.Mod              Mod;
		private CCK.Mods.Events.EventEntryFlags _channel;

		internal EventAPI(ModLoader.Mods.Mod mod, CCK.Mods.Events.EventEntryFlags channel) {
			Mod      = mod;
			_channel = channel;
		}

		private List<EventSubscription> _subscriptions = new();

		// ReSharper disable Unity.PerformanceAnalysis
		private void Receive(EventContext context) {
			var data = new EventData {
				EventName        = context.EventName,
				Data             = context.Data,
				InternalSource   = context.Source,
				SourceChannel    = context.Channel,
				CallbackFunction = context.Callback ?? (_ => { })
			};
			foreach (var sub in _subscriptions)
				if (sub.EventName == null || sub.EventName == context.EventName)
					try {
						sub.Callback(data);
					} catch (Exception e) {
						Logger.LogError($"Error while invoking event callback: {e} of event {context.EventName}");
					}
		}

		private void Emit(EventContext context) {
			var ncontext = new EventContext(context) { CurrentChannel = _channel, Source = Mod };
			var mod      = context.Destination != null ? Mod.CoreAPI.LocalModAPI.GetInternalMod(context.Destination) : null;
			if (mod != null)
				mod.CoreAPI.LocalEventAPI.Receive(ncontext);
			else
				foreach (var imod in Mod.CoreAPI.LocalModAPI.GetInternalMods()) {
					if (context.Channel.HasFlag(CCK.Mods.Events.EventEntryFlags.Main))
						imod.CoreAPI?.LocalEventAPI.Receive(ncontext);
				}
		}

		public void Emit(CCK.Mods.Events.EventContext context)
			=> Emit(new EventContext(context));

		public void Emit(Dictionary<string, object> context)
			=> Emit(EventContext.From(context));

		public void Emit(string eventName, params object[] data) {
			if (data.Length > 0 && data[^1] is Action<object[]>)
				Emit(
					new EventContext {
						Data           = data.Length > 1 ? data[..^1] : Array.Empty<object>(),
						Destination    = null,
						EventName      = eventName,
						Source         = Mod,
						CurrentChannel = _channel,
						Channel        = _channel,
						Callback       = data[^1] as Action<object[]>
					}
				);
			else
				Emit(
					new EventContext {
						Data           = data,
						Destination    = null,
						EventName      = eventName,
						Source         = Mod,
						CurrentChannel = _channel,
						Channel        = _channel,
						Callback       = _ => { }
					}
				);
		}

		public CCK.Mods.Events.EventSubscription Subscribe(string eventName, CCK.Mods.Events.EventCallback callback)
			=> Subscribe(new EventSubscription { EventName = eventName, Callback = callback });

		public CCK.Mods.Events.EventSubscription Subscribe(CCK.Mods.Events.EventSubscription eventSub)
			=> Subscribe(new EventSubscription(eventSub));

		private CCK.Mods.Events.EventSubscription Subscribe(EventSubscription eventSub) {
			if (_subscriptions.Exists(sub => sub.UID == eventSub.UID)) {
				eventSub.UID = 0;
				while (_subscriptions.Exists(sub => sub.UID == eventSub.UID) || eventSub.UID == uint.MaxValue)
					eventSub.UID++;
				if (eventSub.UID == uint.MaxValue)
					return null;
			}

			_subscriptions.Add(eventSub);
			_subscriptions.Sort((a, b) => a.Weight.CompareTo(b.Weight));
			return eventSub;
		}

		public CCK.Mods.Events.EventSubscription Subscribe(Dictionary<string, object> eventSub)
			=> Subscribe(EventSubscription.From(eventSub));

		public void Unsubscribe(CCK.Mods.Events.EventSubscription eventSub)
			=> Unsubscribe(eventSub.UID);

		internal void Unsubscribe(EventSubscription eventSub)
			=> _subscriptions.Remove(eventSub);

		public void Unsubscribe(uint uid)
			=> _subscriptions.RemoveAll(sub => sub.UID == uid);

		public void UnsubscribeAll()
			=> _subscriptions.Clear();

		public void UnsubscribeAll(string eventName)
			=> _subscriptions.RemoveAll(sub => sub.EventName == eventName);
	}

	public class EventSubscription : CCK.Mods.Events.EventSubscription {
		internal EventSubscription() { }

		internal EventSubscription(CCK.Mods.Events.EventSubscription subscription) {
			UID       = subscription.UID;
			EventName = subscription.EventName;
			Weight    = subscription.Weight;
			Callback  = subscription.Callback;
		}

		public uint                          UID       { get; internal set; }
		public string                        EventName { get; internal set; }
		public uint                          Weight    { get; internal set; }
		public CCK.Mods.Events.EventCallback Callback  { get; internal set; }

		public static EventSubscription From(Dictionary<string, object> data) {
			if (data == null) return null;
			return new EventSubscription {
				UID       = data.TryGetValue("uid", out var uid)           && uid is uint u ? u : 0,
				EventName = data.TryGetValue("event_name", out var name)   && name is string n ? n : null,
				Weight    = data.TryGetValue("weight", out var weight)     && weight is uint w ? w : 0,
				Callback  = data.TryGetValue("callback", out var callback) && callback is CCK.Mods.Events.EventCallback cb ? cb : null
			};
		}
	}

	public class EventContext : CCK.Mods.Events.EventContext {
		internal EventContext() { }

		internal EventContext(CCK.Mods.Events.EventContext context) {
			Data        = context.Data;
			Destination = context.Destination;
			EventName   = context.EventName;
			Channel     = context.Channel;
			Callback    = context.Callback;
		}

		public object[]                        Data        { get; internal set; }
		public string                          Destination { get; internal set; }
		public string                          EventName   { get; internal set; }
		public CCK.Mods.Events.EventEntryFlags Channel     { get; internal set; }
		public Action<object[]>                Callback    { get; internal set; }
		public ModLoader.Mods.Mod              Source      { get; internal set; }

		internal CCK.Mods.Events.EventEntryFlags CurrentChannel;

		public static EventContext From(Dictionary<string, object> data) {
			if (data == null) return null;
			return new EventContext {
				Data        = data.TryGetValue("data", out var d)            && d is object[] arr ? arr : Array.Empty<object>(),
				Destination = data.TryGetValue("destination", out var dest)  && dest is string s ? s : null,
				EventName   = data.TryGetValue("event_name", out var name)   && name is string n ? n : null,
				Channel     = data.TryGetValue("channel", out var channel)   && channel is CCK.Mods.Events.EventEntryFlags c ? c : CCK.Mods.Events.EventEntryFlags.Main,
				Source      = data.TryGetValue("source", out var source)     && source is ModLoader.Mods.Mod mod ? mod : null,
				Callback    = data.TryGetValue("callback", out var callback) && callback is Action<object[]> cb ? cb : null
			};
		}
	}

	public class EventData : CCK.Mods.Events.EventData {
		public string                          EventName        { get; internal set; }
		public object[]                        Data             { get; internal set; }
		public CCK.Mods.Events.EventEntryFlags SourceChannel    { get; internal set; }
		public Action<object[]>                CallbackFunction { get; internal set; }

		public bool TryGet<T>(int index, out T value) {
			if (Data == null) {
				value = default;
				return false;
			}


			if (Data.Length > index && Data[index] is T val) {
				value = val;
				return true;
			}

			value = default;
			return false;
		}

		public ModLoader.Mods.Mod InternalSource { get; internal set; }

		public Mod Source
			=> InternalSource;

		public void Callback(params object[] args)
			=> CallbackFunction(args);
	}
}