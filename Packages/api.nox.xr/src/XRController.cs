using System;
using System.Collections.Generic;
using System.Linq;
using Autohand;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.CCK.Players;
using Nox.CCK.Utils;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;
using Transform = UnityEngine.Transform;
using Nox.Controllers;
using Nox.Players;
using Nox.UI;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace api.nox.xr {
	public class XRController : MonoBehaviour, IController, INoxObject {
		private static int DefaultPriority
			=> Client.Instance.IsReady()
				? Config.Load().Get("settings.controller.xr_priority", IController.DefaultPriority + 1)
				: IController.DefaultPriority - 1;

		private const string DefaultId = "xr";

		/// <summary>
		/// Get the proxy mod API.
		/// </summary>
		private static IControllerAPI ControllerAPI
			=> Client.CoreAPI.ModAPI
				.GetMod("controller")
				?.GetEntry<IControllerAPI>();

		/// <summary>
		/// Check if the current proxy is better than XR proxy.
		/// </summary>
		/// <returns></returns>
		private static bool IsBetterThanCurrent() {
			var controller = ControllerAPI.GetCurrent();
			return controller               == null
				|| controller.GetPriority() < DefaultPriority
				|| controller.GetId()       == DefaultId;
		}

		/// <summary>
		/// Check if the current proxy is the XR proxy.
		/// </summary>
		/// <returns></returns>
		private static bool IsCurrent() {
			var controller = ControllerAPI.GetCurrent();
			return controller         != null
				&& controller.GetId() == DefaultId;
		}

		/// <summary>
		/// Remove the current proxy if it is the XR proxy.
		/// </summary>
		internal static bool Remove() {
			if (!IsCurrent()) return false;
			ControllerAPI.SetCurrent(null);
			return true;
		}

		/// <summary>
		/// Create the XR proxy if it is not already created.
		/// </summary>
		/// <returns></returns>
		internal static bool Make() {
			if (!IsBetterThanCurrent()) {
				Logger.LogDebug(
					"XR proxy is not better than current controller, skipping creation\n"
					+ $"Current: {ControllerAPI.GetCurrent()?.GetId() ?? "null"} ({ControllerAPI.GetCurrent()?.GetPriority() ?? -1})\n"
					+ $"XR: {DefaultId} ({DefaultPriority})"
					+ $" - {(Client.Instance.IsReady() ? "XR Ready" : "XR Not Ready")}"
					+ $" - {(Client.Instance.HasHeadset() ? "Has Headset" : "No Headset")}"
					+ $" ({(Client.Instance.IsXRInitialized() ? "XR Initialized" : "XR Not Initialized")})"
				);
				return false;
			}

			var prefab = Client.CoreAPI.AssetAPI.GetAsset<GameObject>("xr_proxy.prefab");
			if (!prefab) {
				Logger.LogError("Failed to load desktop proxy prefab");
				return false;
			}

			var instance = Instantiate(prefab);
			var xr       = instance.GetComponent<XRController>();

			if (!xr) {
				Logger.LogError("Failed to get desktop proxy component");
				Destroy(instance);
				return false;
			}

			Logger.LogDebug("Menu: " + Client.UiAPI);
			xr.Menu = Client.UiAPI
				?.Make(xr.menuContainer, xr.menuParent);

			if (xr.Menu == null) {
				Logger.LogError("Failed to create desktop proxy menu");
				Destroy(instance);
				return false;
			}

			xr.Menu.SetActive(false);


			if (!ControllerAPI.SetCurrent(xr)) {
				Logger.LogError("Failed to set XR proxy as current");
				Destroy(instance);
				return false;
			}

			EventSystem.current = xr.eventSystem;
			xr.gameObject.name  = $"[{xr.GetType().Name}_{xr.GetInstanceID()}]";
			DontDestroyOnLoad(xr);
			return true;
		}

		[NoxPublic(NoxAccess.Method)]
		public string GetId()
			=> DefaultId;

		[NoxPublic(NoxAccess.Method)]
		public int GetPriority()
			=> DefaultPriority;

		public  AutoHandPlayer       player;
		public  bool                 mayFly;
		public  RectTransform        menuContainer;
		public  GameObject           menuParent;
		public  IMenu                Menu;
		public  EventSystem          eventSystem;
		private IPlayer              _attachedPlayer;
		public  XRInteractionGroup[] interactions;

		public void Dispose() {
			Destroy(gameObject);
		}

		[NoxPublic(NoxAccess.Method)]
		public Camera GetCamera()
			=> player.headCamera;

		[NoxPublic(NoxAccess.Method)]
		public Collider GetCollider()
			=> player.bodyCollider;

		public void Restore(IController controller) {
			foreach (var ability in controller.GetAbilities())
				SetAbilities(ability.Key, ability.Value);
			var p = controller.GetPlayer();
			controller.SetPlayer(null);
			SetPlayer(p);
		}

		[NoxPublic(NoxAccess.Method)]
		public Dictionary<string, object> GetAbilities()
			=> new() {
				{ "pushing", player.IsPushing() },
				{ "grounded", player.IsGrounded() },
				{ "climbing", player.IsClimbing() },
				{ "pushing_up", player.IsPushingUp() },
				{ "immobilized", player.useMovement },
				{ "crouching", player.crouching },
				{ "flying", IsFlying },
				{ "may_fly", MayFly },
				{ "max_move_speed", player.maxMoveSpeed },
				{ "move_acceleration", player.moveAcceleration }
			};

		[NoxPublic(NoxAccess.Method)]
		public void SetAbilities(string key, object value) {
			if (!GetAbilities().ContainsKey(key)) return;
			switch (key) {
				case "immobilized":
					player.useMovement = (bool)value;
					break;
				case "crouching":
					player.crouching = (bool)value;
					break;
				case "flying":
					IsFlying = (bool)value;
					break;
				case "may_fly":
					MayFly = (bool)value;
					break;
			}
		}

		[NoxPublic(NoxAccess.Method)]
		public Dictionary<ushort, Transform> GetParts()
			=> new() {
				{ PlayerRig.Base.ToIndex(), transform },
				{ PlayerRig.Head.ToIndex(), player.headCamera.transform },
				{ PlayerRig.LeftHand.ToIndex(), player.handLeft.transform },
				{ PlayerRig.RightHand.ToIndex(), player.handRight.transform }
			};


		[NoxPublic(NoxAccess.Method)]
		public void SetPlayer(IPlayer p) {
			_attachedPlayer = p;
			Client.CoreAPI.EventAPI.Emit("controller_set_player", this, _attachedPlayer);
			if (p == null) return;
			SynchronizeControllerFromPlayer();
		}

		public IRuntimeAvatar GetAvatar() {
			throw new NotImplementedException();
		}

		public void SetAvatar(IRuntimeAvatar runtimeAvatar) {
			throw new NotImplementedException();
		}

		[NoxPublic(NoxAccess.Method)]
		public IPlayer GetPlayer()
			=> _attachedPlayer;

		private void Start()
			=> RestartInteractions().Forget();

		private async UniTask RestartInteractions() {
			foreach (var interaction in interactions) {
				interaction.gameObject.SetActive(false);
				foreach (var member in interaction.startingGroupMembers)
					if (member is MonoBehaviour mb)
						mb.gameObject.SetActive(false);
			}

			await UniTask.NextFrame();

			foreach (var interaction in interactions) {
				interaction.gameObject.SetActive(true);
				foreach (var member in interaction.startingGroupMembers)
					if (member is MonoBehaviour mb)
						mb.gameObject.SetActive(true);
			}
		}

		private void Update() {
			SynchronizePlayerFromController();
		}

		private void SynchronizePlayerFromController() {
			_attachedPlayer?.SetPosition(transform.position);
		}

		private void SynchronizeControllerFromPlayer() {
			if (_attachedPlayer == null) return;
			transform.position = _attachedPlayer.GetPosition();
		}

		/// <summary>
		/// When the player is currently flying, the value is true.
		/// </summary>
		private bool IsFlying {
			get => !player.useGrounding;
			set {
				if (IsFlying != value) player.ToggleFlying();
			}
		}


		/// <summary>
		/// When the player can fly, the value is true.
		/// This is a toggle, so if you set it to false,
		/// the player will not be able to fly
		/// (if it was flying, it will stop flying).
		/// </summary>
		private bool MayFly {
			get => mayFly;
			set {
				if (mayFly == value) return;
				mayFly = value;
				if (IsFlying && !mayFly)
					IsFlying = false;
			}
		}
	}
}