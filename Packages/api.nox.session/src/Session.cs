using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using Nox.Entities;
using Nox.Players;
using Nox.Sessions;
using Nox.Worlds;
using UnityEngine;
using UnityEngine.Events;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.session {
	public class Session : ISession, INoxObject, ISessionEvents {
		public Session(Main manager, ushort id, IAdapter adapter) {
			Id      = id;
			Adapter = adapter;
			Adapter.SetSession(this);
			Manager = manager;
			Manager.Add(this);
		}

		public readonly ushort   Id;
		public readonly IAdapter Adapter;
		public readonly Main     Manager;

		public readonly UnityEvent<IPlayer>                      OnPlayerJoinedListener         = new();
		public readonly UnityEvent<IPlayer>                      OnPlayerLeftListener           = new();
		public readonly UnityEvent<IPlayer>                      OnAuthorityTransferredListener = new();
		public readonly UnityEvent<IAdapterState, IAdapterState> OnStateChangedListener         = new();
		public readonly UnityEvent<IEntity>                      OnEntityRegisteredListener     = new();
		public readonly UnityEvent<IEntity>                      OnEntityUnregisteredListener   = new();
		public readonly UnityEvent<string, byte[], IPlayer>      OnEventTriggeredListener       = new();

		[NoxPublic(NoxAccess.Method)]
		public ushort GetId()
			=> Id;

		[NoxPublic(NoxAccess.Method)]
		public IAdapter GetAdapter()
			=> Adapter;

		[NoxPublic(NoxAccess.Method)]
		public async UniTask SetCurrent()
			=> await Manager.SetCurrent(Id);

		[NoxPublic(NoxAccess.Method)]
		public bool IsCurrent()
			=> Manager.CurrentId == Id;

		[NoxPublic(NoxAccess.Method)]
		public IPlayer GetPlayer(int id)
			=> Adapter.GetPlayer(id);

		[NoxPublic(NoxAccess.Method)]
		public IEntity GetEntity(int id)
			=> Adapter.GetEntity(id);

		[NoxPublic(NoxAccess.Method)]
		public int GetEntityCount()
			=> Adapter.GetEntityCount();

		[NoxPublic(NoxAccess.Method)]
		public int GetPlayerCount()
			=> Adapter.GetPlayerCount();

		[NoxPublic(NoxAccess.Method)]
		public async UniTask Dispose() {
			await Adapter.Dispose();
			Manager.Remove(this);
		}

		public void OnPlayerJoined(IPlayer player) {
			Logger.LogDebug($"OnPlayerJoined: {player}");

			// Si c'est un joueur local, vérifier s'il faut le téléporter au spawn
			if (player.IsLocal())
				player.Respawn();

			Main.Instance.CoreAPI.EventAPI.Emit("session_player_joined", this, player);
			OnPlayerJoinedListener.Invoke(player);

			foreach (var descriptor in GetDescriptors().Where(e => e != null))
			foreach (var module in descriptor.GetModules<ISessionModule>())
				module.OnPlayerJoined(player);
		}

		public void OnPlayerLeft(IPlayer player) {
			Logger.LogDebug($"OnPlayerLeft: {player}");

			Main.Instance.CoreAPI.EventAPI.Emit("session_player_left", this, player);
			OnPlayerLeftListener.Invoke(player);

			foreach (var descriptor in GetDescriptors().Where(e => e != null))
			foreach (var module in descriptor.GetModules<ISessionModule>())
				module.OnPlayerLeft(player);
		}

		public void OnAuthorityTransferred(IPlayer player) {
			Logger.LogDebug($"OnAuthorityTransferred: {player}");

			Main.Instance.CoreAPI.EventAPI.Emit("session_authority_transferred", this, player);
			OnAuthorityTransferredListener.Invoke(player);

			foreach (var descriptor in GetDescriptors().Where(e => e != null))
			foreach (var module in descriptor.GetModules<ISessionModule>())
				module.OnAuthorityTransferred(player);
		}

		public void OnEntityRegistered(IEntity entity) {
			Logger.LogDebug($"OnEntityRegistered: {entity}");

			Main.Instance.CoreAPI.EventAPI.Emit("session_entity_registered", this, entity);
			OnEntityRegisteredListener.Invoke(entity);

			foreach (var descriptor in GetDescriptors().Where(e => e != null))
			foreach (var module in descriptor.GetModules<ISessionModule>())
				module.OnEntityRegistered(entity);
		}

		public void OnEntityUnregistered(IEntity entity) {
			Logger.LogDebug($"OnEntityUnregistered: {entity}");

			Main.Instance.CoreAPI.EventAPI.Emit("session_entity_unregistered", this, entity);
			OnEntityUnregisteredListener.Invoke(entity);

			foreach (var descriptor in GetDescriptors().Where(e => e != null))
			foreach (var module in descriptor.GetModules<ISessionModule>())
				module.OnEntityUnregistered(entity);
		}

		public void OnEventTriggered(string @event, byte[] raw, IPlayer sender) {
			Logger.LogDebug($"OnEventTriggered: {@event} by {sender}");

			Main.Instance.CoreAPI.EventAPI.Emit("session_event_triggered", this, @event, raw, sender);
			OnEventTriggeredListener.Invoke(@event, raw, sender);
			
			foreach (var descriptor in GetDescriptors().Where(e => e != null))
			foreach (var module in descriptor.GetModules<ISessionModule>())
				module.OnEventTriggered(@event, raw, sender);
		}

		public IWorldDescriptor[] GetDescriptors() {
			var dimension = Adapter.GetDimension();
			if (dimension == null) {
				Logger.LogWarning($"{this}: GetDescriptors called before a dimension was assigned. Returning no descriptors.");
				return Array.Empty<IWorldDescriptor>();
			}

			var scene = dimension.GetScene();
			if (scene == null) {
				Logger.LogWarning($"{this}: GetDescriptors called but dimension has no scene. Returning no descriptors.");
				return Array.Empty<IWorldDescriptor>();
			}

			var instances = scene.GetInstances();
			if (instances == null || instances.Length == 0) {
				Logger.LogWarning($"{this}: GetDescriptors found no instances in the scene. Returning no descriptors.");
				return Array.Empty<IWorldDescriptor>();
			}

			var mainInstance = instances[0];
			if (mainInstance == null) {
				Logger.LogWarning($"{this}: GetDescriptors found a null main instance. Returning no descriptors.");
				return Array.Empty<IWorldDescriptor>();
			}

			var mainIndex  = dimension.GetMainIndex();
			var descriptor = mainInstance.GetDescriptor(mainIndex);
			return descriptor != null
				? new[] { descriptor }
				: Array.Empty<IWorldDescriptor>();
		}

		public void OnStateChanged(IAdapterState state, IAdapterState previousState) {
			Main.Instance.CoreAPI.EventAPI.Emit("session_state_changed", this, state, previousState);
			if (state.IsReady() && !previousState.IsReady())
				Main.Instance.CoreAPI.EventAPI.Emit("session_ready", this);
			else if (!state.IsReady() && Mathf.Approximately(state.GetProgress(), -1))
				Main.Instance.CoreAPI.EventAPI.Emit("session_error", this);
			OnStateChangedListener.Invoke(state, previousState);
		}

		public void OnUpdate()
			=> Adapter.OnUpdate();

		public bool Match(IWorldIdentifier identifier)
			=> Adapter.GetDimension()?.GetScene().GetIdentifier().Equals(identifier)
				?? false;

		public override string ToString()
			=> $"{GetType().Name}[Id={Id}, Adapter={Adapter}]";

		public async UniTask OnDeselect(ISession nSession) {
			foreach (var descriptor in GetDescriptors().Where(e => e != null))
			foreach (var module in descriptor.GetModules<ISessionModule>())
				module.OnSessionDeselected();

			await Adapter.OnDeselect(nSession);
		}

		public void OnSceneLoaded(IWorldDescriptor descriptor, int index, GameObject anchor) {
			Main.Instance.CoreAPI.EventAPI.Emit("session_scene_added", this, index, descriptor, anchor);

			var modules = descriptor.GetModules<ISessionModule>();
			Logger.LogDebug($"OnDescriptorAdded: {descriptor} with {modules.Length} modules");

			foreach (var module in modules)
				module.OnLoaded(this);

			for (var i = 0; i < GetPlayerCount(); i++)
				foreach (var module in modules)
					module.OnPlayerJoined(GetPlayer(i));

			var master = Adapter.GetMasterPlayer();
			foreach (var module in modules)
				module.OnAuthorityTransferred(master);

			if (IsCurrent())
				foreach (var module in modules)
					module.OnSessionSelected();

			var dimension = Adapter.GetDimension();
			for (var i = 0; i < dimension.GetSize(); i++) {
				var d = dimension.GetDescriptor(i);
				if (d == null) continue;
				foreach (var module in d.GetModules<ISessionModule>())
					module.OnSceneLoaded(descriptor, index, anchor);
			}
		}

		public void OnSceneUnloaded(int index) {
			Main.Instance.CoreAPI.EventAPI.Emit("session_scene_removed", this, index);
			var dimension = Adapter.GetDimension();
			for (var i = 0; i < dimension.GetSize(); i++) {
				var d = dimension.GetDescriptor(i);
				if (d == null) continue;
				foreach (var module in d.GetModules<ISessionModule>())
					module.OnSceneUnloaded(index);
			}
		}

		public IDimension GetDimension()
			=> Adapter.GetDimension();

		public async UniTask OnSelect(ISession oSession) {
			await Adapter.OnSelect(oSession);

			foreach (var descriptor in GetDescriptors().Where(e => e != null))
			foreach (var module in descriptor.GetModules<ISessionModule>())
				module.OnSessionSelected();
		}

		public UnityEvent<IPlayer> OnPlayerJoinedEvent()
			=> OnPlayerJoinedListener;

		public UnityEvent<IPlayer> OnPlayerLeftEvent()
			=> OnPlayerLeftListener;

		public UnityEvent<IAdapterState, IAdapterState> OnStateChangedEvent()
			=> OnStateChangedListener;

		public UnityEvent<IEntity> OnEntityRegisteredEvent()
			=> OnEntityRegisteredListener;

		public UnityEvent<IEntity> OnEntityUnregisteredEvent()
			=> OnEntityUnregisteredListener;

		public UnityEvent<IPlayer> OnAuthorityTransferredEvent()
			=> OnAuthorityTransferredListener;
	}
}