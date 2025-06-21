using System;
using System.Collections.Generic;
using System.Linq;
using Autohand;
using Nox.CCK.Players;
using Nox.CCK.Utils;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;
using Transform = UnityEngine.Transform;
using Nox.Controllers;
using Nox.Players;

namespace api.nox.xr {
	public class XRController : MonoBehaviour, IController, INoxObject {
		private static int DefaultPriority
			=> Client.IsReady() && Client.Instance.HasHeadset()
				? Config.Load().Get("settings.controller.xr_priority", IController.DefaultPriority + 1)
				: IController.DefaultPriority - 1;

		private const string DefaultId = "xr";

		/// <summary>
		/// Get the proxy mod API.
		/// </summary>
		private static IControllerAPI ControllerAPI
			=> Client.CoreAPI.ModAPI.GetMod("controller").GetMains().FirstOrDefault() as IControllerAPI;

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
			if (!IsBetterThanCurrent()) return false;

			var prefab = Client.CoreAPI.AssetAPI.GetAsset<GameObject>("proxy.prefab");
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

			if (!ControllerAPI.SetCurrent(xr)) {
				Logger.LogError("Failed to set XR proxy as current");
				Destroy(instance);
				return false;
			}

			xr.gameObject.name = $"[{xr.GetType().Name}_{xr.GetInstanceID()}]";
			DontDestroyOnLoad(xr);
			return true;
		}

		[NoxPublic(NoxAccess.Method)]
		public string GetId()
			=> DefaultId;

		[NoxPublic(NoxAccess.Method)]
		public int GetPriority()
			=> DefaultPriority;

		public AutoHandPlayer player;
		public bool           mayFly;

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
		
		private IPlayer _attachedPlayer;

		[NoxPublic(NoxAccess.Method)]
		public void SetPlayer(IPlayer p) {
			_attachedPlayer = p;
			if (p == null) return;
			SynchronizeControllerFromPlayer();
		}

		[NoxPublic(NoxAccess.Method)]
		public IPlayer GetPlayer()
			=> _attachedPlayer;

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