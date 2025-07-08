using System;
using Nox.CCK.Mods.Events;
using Nox.CCK.Utils;
using UnityEngine.Events;

namespace api.nox.world.jint {
	public class WorldManager {
		internal static INoxObject BaseManager
			=> Main.WorldAPI.GetField("Manager");

		private EventSubscription[] _events;

		internal WorldManager() {
			_events = new[] {
				Main.CoreAPI.EventAPI.Subscribe("world_added", Internal_OnWorldAdded),
				Main.CoreAPI.EventAPI.Subscribe("world_removed", Internal_OnWorldRemoved)
			};
		}

		internal void Dispose() {
			if (_events == null) return;
			foreach (var subscription in _events)
				Main.CoreAPI.EventAPI.Unsubscribe(subscription);
			_events = null;
		}

		private void Internal_OnWorldAdded(EventData context) {
			var world = context.TryGet(0, out INoxObject w) ? w : null;
			if (world == null) return;

			// Handle world added logic here
			Logger.LogDebug($"World added: {world}");
			var l = world.CallMethod<int>("GetSceneCount");
			for (var i = 0; i < l; i++) {
				var scene = world.CallMethod<INoxObject>("GetScene", i);
				if (scene == null) continue;
				Logger.LogDebug($"Scene {i} added to world: {scene}");
			}
		}

		private void Internal_OnWorldRemoved(EventData context) {
			var world = context.TryGet(0, out INoxObject w) ? w : null;
			if (world == null) return;
			// Handle world removed logic here
			Logger.LogDebug($"World removed: {world}");
		}
	}
}