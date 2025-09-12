using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using Nox.CCK.Worlds;
using Nox.Entities;
using Nox.Players;
using Nox.Sessions;
using Nox.Worlds;
using Nox.Worlds.Spawns;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
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

		public readonly UnityEvent<IPlayer>                      OnPlayerJoinedEvent         = new();
		public readonly UnityEvent<IPlayer>                      OnPlayerLeftEvent           = new();
		public readonly UnityEvent<IPlayer>                      OnAuthorityTransferredEvent = new();
		public readonly UnityEvent<IAdapterState, IAdapterState> OnStateChangedEvent         = new();

		public void AddPlayerJoinedListener(UnityAction<IPlayer> action)
			=> OnPlayerJoinedEvent.AddListener(action);

		public void AddPlayerLeftListener(UnityAction<IPlayer> action)
			=> OnPlayerLeftEvent.AddListener(action);

		public void AddAuthorityTransferredListener(UnityAction<IPlayer> action)
			=> OnAuthorityTransferredEvent.AddListener(action);

		public void AddStateChangedListener(UnityAction<IAdapterState, IAdapterState> action)
			=> OnStateChangedEvent.AddListener(action);

		public void RemovePlayerJoinedListener(UnityAction<IPlayer> action)
			=> OnPlayerJoinedEvent.RemoveListener(action);

		public void RemovePlayerLeftListener(UnityAction<IPlayer> action)
			=> OnPlayerLeftEvent.RemoveListener(action);

		public void RemoveAuthorityTransferredListener(UnityAction<IPlayer> action)
			=> OnAuthorityTransferredEvent.RemoveListener(action);

		public void RemoveStateChangedListener(UnityAction<IAdapterState, IAdapterState> action)
			=> OnStateChangedEvent.RemoveListener(action);

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
			OnPlayerJoinedEvent.Invoke(player);

			foreach (var descriptor in GetDescriptors().Where(e => e != null))
			foreach (var module in descriptor.GetModules<ISessionModule>())
				module.OnPlayerJoined(player);
		}

		public void OnPlayerLeft(IPlayer player) {
			Logger.LogDebug($"OnPlayerLeft: {player}");
			Main.Instance.CoreAPI.EventAPI.Emit("session_player_left", this, player);
			OnPlayerLeftEvent.Invoke(player);

			foreach (var descriptor in GetDescriptors().Where(e => e != null))
			foreach (var module in descriptor.GetModules<ISessionModule>())
				module.OnPlayerLeft(player);
		}

		public void OnAuthorityTransferred(IPlayer player) {
			Logger.LogDebug($"OnAuthorityTransferred: {player}");
			Main.Instance.CoreAPI.EventAPI.Emit("session_authority_transferred", this, player);
			OnAuthorityTransferredEvent.Invoke(player);

			foreach (var descriptor in GetDescriptors().Where(e => e != null))
			foreach (var module in descriptor.GetModules<ISessionModule>())
				module.OnAuthorityTransferred(player);
		}

		public IWorldDescriptor[] GetDescriptors() {
			var dimension = Adapter.GetDimension();
			var main = dimension.GetScene()
				.GetInstances()[0]
				.GetInstanceDescriptor(dimension.GetMainIndex());
			return new[] { main };
		}

		public void OnStateChanged(IAdapterState state, IAdapterState previousState) {
			Main.Instance.CoreAPI.EventAPI.Emit("session_state_changed", this, state, previousState);
			if (state.IsReady() && !previousState.IsReady())
				Main.Instance.CoreAPI.EventAPI.Emit("session_ready", this);
			else if (!state.IsReady() && Mathf.Approximately(state.GetProgress(), -1))
				Main.Instance.CoreAPI.EventAPI.Emit("session_error", this);
			OnStateChangedEvent.Invoke(state, previousState);
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

		public void OnDescriptorAdded(IWorldDescriptor descriptor) {
			Main.Instance.CoreAPI.EventAPI.Emit("session_descriptor_added", this, descriptor);

			var modules = descriptor.GetModules<ISessionModule>();
			Logger.LogDebug($"OnDescriptorAdded: {descriptor} with {modules.Length} modules");

			foreach (var module in modules)
				module.OnSession(this);

			for (var i = 0; i < GetPlayerCount(); i++)
				foreach (var module in modules)
					module.OnPlayerJoined(GetPlayer(i));

			var master = Adapter.GetMasterPlayer();
			foreach (var module in modules)
				module.OnAuthorityTransferred(master);

			if (IsCurrent())
				foreach (var module in modules)
					module.OnSessionSelected();
		}

		public async UniTask OnSelect(ISession oSession) {
			await Adapter.OnSelect(oSession);

			foreach (var descriptor in GetDescriptors().Where(e => e != null))
			foreach (var module in descriptor.GetModules<ISessionModule>())
				module.OnSessionSelected();
		}
	}
}