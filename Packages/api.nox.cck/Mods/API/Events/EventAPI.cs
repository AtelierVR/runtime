using System;
using System.Collections.Generic;

namespace Nox.CCK.Mods.Events {
	/// <summary>
	/// API for managing events, allowing mods to emit and subscribe to custom events.
	/// </summary>
	public interface IEventAPI {
		/// <summary>
		/// Emits an event with the specified name and data.
		/// </summary>
		/// <param name="eventName">The name of the event to emit.</param>
		/// <param name="data">The data to pass with the event.</param>
		public void Emit(string                     eventName, params object[] data);
		
		/// <summary>
		/// Emits an event using an event context.
		/// </summary>
		/// <param name="context">The event context containing event details.</param>
		public void Emit(EventContext               context);
		
		/// <summary>
		/// Emits an event using a dictionary-based context.
		/// </summary>
		/// <param name="context">A dictionary containing event context information.</param>
		public void Emit(Dictionary<string, object> context);

		/// <summary>
		/// Subscribes to an event with the specified name and callback.
		/// </summary>
		/// <param name="eventName">The name of the event to subscribe to.</param>
		/// <param name="callback">The callback to invoke when the event is emitted.</param>
		/// <returns>An event subscription object.</returns>
		public EventSubscription Subscribe(string                     eventName, EventCallback callback);
		
		/// <summary>
		/// Subscribes to an event using an event subscription object.
		/// </summary>
		/// <param name="eventSub">The event subscription.</param>
		/// <returns>An event subscription object.</returns>
		public EventSubscription Subscribe(EventSubscription          eventSub);
		
		/// <summary>
		/// Subscribes to an event using a dictionary-based subscription.
		/// </summary>
		/// <param name="eventSub">A dictionary containing subscription information.</param>
		/// <returns>An event subscription object.</returns>
		public EventSubscription Subscribe(Dictionary<string, object> eventSub);
		
		/// <summary>
		/// Unsubscribes from an event using an event subscription object.
		/// </summary>
		/// <param name="eventSub">The event subscription to remove.</param>
		public void              Unsubscribe(EventSubscription        eventSub);
		
		/// <summary>
		/// Unsubscribes from an event using its unique identifier.
		/// </summary>
		/// <param name="uid">The unique identifier of the subscription.</param>
		public void              Unsubscribe(uint                     uid);
		
		/// <summary>
		/// Unsubscribes from all events.
		/// </summary>
		public void              UnsubscribeAll();
		
		/// <summary>
		/// Unsubscribes from all subscriptions for a specific event name.
		/// </summary>
		/// <param name="eventName">The name of the event.</param>
		public void              UnsubscribeAll(string eventName);
	}

	/// <summary>
	/// Delegate for event callback functions.
	/// </summary>
	/// <param name="context">The event data context.</param>
	public delegate void EventCallback(EventData context);

	/// <summary>
	/// Represents an event subscription.
	/// </summary>
	public interface EventSubscription {
		/// <summary>
		/// Gets the unique identifier of the subscription.
		/// </summary>
		public uint          UID       { get; }
		
		/// <summary>
		/// Gets the name of the event being subscribed to.
		/// </summary>
		public string        EventName { get; }
		
		/// <summary>
		/// Gets the priority weight of the subscription (higher weights are called first).
		/// </summary>
		public uint          Weight    { get; }
		
		/// <summary>
		/// Gets the callback function for the subscription.
		/// </summary>
		public EventCallback Callback  { get; }
	}

	/// <summary>
	/// Represents event data passed to event callbacks.
	/// </summary>
	public interface EventData {
		/// <summary>
		/// Gets the name of the event.
		/// </summary>
		public string          EventName { get; }
		
		/// <summary>
		/// Gets the data array passed with the event.
		/// </summary>
		public object[]        Data      { get; }
		
		/// <summary>
		/// Gets the mod that emitted the event.
		/// </summary>
		public IMod             Source    { get; }
		
		/// <summary>
		/// Invokes a callback with the specified arguments.
		/// </summary>
		/// <param name="args">The arguments to pass to the callback.</param>
		public void            Callback(params object[] args);
		
		/// <summary>
		/// Gets the source channel flags of the event.
		/// </summary>
		public EventEntryFlags SourceChannel { get; }

		/// <summary>
		/// Tries to get a value from the data array at the specified index.
		/// </summary>
		/// <typeparam name="T">The type of the value to retrieve.</typeparam>
		/// <param name="index">The index in the data array.</param>
		/// <param name="value">The retrieved value, if successful.</param>
		/// <returns>True if the value was successfully retrieved; otherwise, false.</returns>
		public bool TryGet<T>(int index, out T value);
	}

	/// <summary>
	/// Represents the context for emitting an event.
	/// </summary>
	public interface EventContext {
		/// <summary>
		/// Gets the data to pass with the event.
		/// </summary>
		public object[]         Data        { get; }
		
		/// <summary>
		/// Gets the destination for the event.
		/// </summary>
		public string           Destination { get; }
		
		/// <summary>
		/// Gets the name of the event.
		/// </summary>
		public string           EventName   { get; }
		
		/// <summary>
		/// Gets the channel flags for the event.
		/// </summary>
		public EventEntryFlags  Channel     { get; }
		
		/// <summary>
		/// Gets the callback action for the event.
		/// </summary>
		public Action<object[]> Callback    { get; }
	}

	/// <summary>
	/// Flags indicating which channels an event applies to.
	/// </summary>
	[Flags]
	public enum EventEntryFlags {
		/// <summary>Main application channel.</summary>
		Main     = 1,
		/// <summary>Client channel.</summary>
		Client   = 2,
		/// <summary>Instance channel.</summary>
		Instance = 4,
		/// <summary>Editor channel.</summary>
		Editor   = 8,
		Server   = 16,
		Custom   = 32,
		Internal = Main | Client | Editor   | Server,
		All      = Main | Client | Instance | Editor | Server | Custom
	}
}