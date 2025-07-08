using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using Nox.CCK.Worlds;
using Nox.Entities;
using Nox.Players;
using Nox.Sessions;
using UnityEngine.SceneManagement;

namespace api.nox.session {
	public class Session : ISession, INoxObject {
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
			=> Manager.GetCurrent()?.GetId() == Id;

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
				TryTeleportPlayerToSpawn(player);

			Main.Instance.CoreAPI.EventAPI.Emit("session_player_joined", this, player);
		}

		public void OnPlayerLeft(IPlayer player) {
			Logger.LogDebug($"OnPlayerLeft: {player}");
			Main.Instance.CoreAPI.EventAPI.Emit("session_player_left", this, player);
		}

		public void OnAuthorityTransferred(IPlayer player) {
			Logger.LogDebug($"OnAuthorityTransferred: {player}");
			Main.Instance.CoreAPI.EventAPI.Emit("session_authority_transferred", this, player);
		}

		public override string ToString()
			=> $"{GetType().Name}[Id={Id}, Adapter={Adapter}]";

		public async UniTask OnDeselect(Session nSession)
			=> await Adapter.OnDeselect(nSession);

		public async UniTask OnSelect(Session oSession)
			=> await Adapter.OnSelect(oSession);

		private void TryTeleportPlayerToSpawn(IPlayer player) {
			// Rechercher un descripteur de scène dans la scène active
			var activeScene = SceneManager.GetActiveScene();
			if (!SceneDescriptorExtension.TryGetDescriptor<BaseSceneDescriptor>(activeScene, out var descriptor)) {
				Logger.LogDebug("No scene descriptor found in active scene for spawn teleportation");
				return;
			}

			// Vérifier si le descripteur utilise un système de spawn
			if (!descriptor.UseSpawn()) {
				Logger.LogDebug("Scene descriptor does not use spawn system");
				return;
			}

			// Choisir un spawn et téléporter le joueur
			var spawnObject = descriptor.ChoiceSpawn();
			if (spawnObject) {
				player.Teleport(spawnObject.transform);
				Logger.LogDebug($"Teleported local player {player.GetDisplay()} to spawn at {spawnObject.transform.position}");
			} else {
				Logger.LogWarning("ChoiceSpawn returned null object");
			}
		}
	}
}