using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.Avatars.Camera;
using Nox.Avatars.Parameters;
using Nox.CCK.Players;
using Nox.CCK.Utils;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;
using Transform = UnityEngine.Transform;
using Nox.Controllers;
using Nox.Players;
using UnityEngine.EventSystems;
using NoxTransform = Nox.CCK.Utils.Transform;

namespace api.nox.desktop {
	public class DesktopController : MonoBehaviour, IController, INoxObject {
		private static int DefaultPriority
			=> Config.Load().Get("settings.controller.desktop_priority", IController.DefaultPriority);

		private const string DefaultId = "desktop";

		/// <summary>
		/// Get the proxy mod API.
		/// </summary>
		private static IControllerAPI ControllerAPI
			=> Client.CoreAPI.ModAPI.GetMod("controller").GetMains().FirstOrDefault() as IControllerAPI;

		/// <summary>
		/// Check if the current proxy is better than Desktop proxy.
		/// </summary>
		/// <returns></returns>
		private static bool IsBetterThanCurrent() {
			var controller = ControllerAPI.GetCurrent();
			return controller               == null
				|| controller.GetPriority() < DefaultPriority
				|| controller.GetId()       == DefaultId;
		}

		/// <summary>
		/// Check if the current proxy is the Desktop proxy.
		/// </summary>
		/// <returns></returns>
		private static bool IsCurrent() {
			var controller = ControllerAPI.GetCurrent();
			return controller         != null
				&& controller.GetId() == DefaultId;
		}

		/// <summary>
		/// Remove the current proxy if it is the Desktop proxy.
		/// </summary>
		internal static bool Remove() {
			if (!IsCurrent()) return false;
			ControllerAPI.SetCurrent(null);
			return true;
		}

		/// <summary>
		/// Create the Desktop proxy if it is not already created.
		/// </summary>
		/// <returns></returns>
		internal static bool Make() {
			if (!IsBetterThanCurrent()) return false;

			var prefab = Client.CoreAPI.AssetAPI.GetAsset<GameObject>("desktop_proxy.prefab");
			if (!prefab) {
				Logger.LogError("Failed to load desktop proxy prefab");
				return false;
			}

			var instance = Instantiate(prefab);
			var desktop  = instance.GetComponent<DesktopController>();

			if (!desktop) {
				Logger.LogError("Failed to get desktop proxy component");
				Destroy(instance);
				return false;
			}

			Logger.LogDebug("Menu: " + Client.UiAPI);
			desktop.player.menu = Client.UiAPI
				?.Make(desktop.player.menuContainer);

			if (desktop.player.menu == null) {
				Logger.LogError("Failed to create desktop proxy menu");
				Destroy(instance);
				return false;
			}

			desktop.player.menu.SetActive(false);

			if (!ControllerAPI.SetCurrent(desktop)) {
				Logger.LogError("Failed to set Desktop proxy as current");
				Destroy(instance);
				return false;
			}

			if (desktop._attachedAvatar == null)
				desktop.SetupAvatar().Forget();

			EventSystem.current     = desktop.eventSystem;
			desktop.gameObject.name = $"[{desktop.GetType().Name}_{desktop.GetInstanceID()}]";
			DontDestroyOnLoad(desktop);
			return true;
		}


		[NoxPublic(NoxAccess.Method)]
		public string GetId()
			=> DefaultId;

		[NoxPublic(NoxAccess.Method)]
		public int GetPriority()
			=> DefaultPriority;

		public DesktopPlayer player;
		public EventSystem   eventSystem;

		public void Dispose() {
			Destroy(gameObject);
			_attachedAvatar?.Dispose();
		}

		private async UniTask SetupAvatar() {
			if (_attachedAvatar != null) {
				Logger.LogDebug("Avatar already set for DesktopController");
				return;
			}

			Logger.LogDebug("Creating avatar");

			var avatar = await Client.AvatarAPI.MakeLoading();
			if (avatar == null) {
				Logger.LogError("Failed to create avatar for DesktopController");
				return;
			}

			SetAvatar(avatar);
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
			SetAvatar(controller.GetAvatar());
			controller.SetAvatar(null);
		}

		[NoxPublic(NoxAccess.Method)]
		public Dictionary<string, object> GetAbilities()
			=> new() {
				{ "grounded", player.IsGrounded() },
				{ "immobilized", !player.useMovement },
				{ "crouching", player.crouching },
				{ "sprinting", player.IsSprinting() },
				{ "flying", player.IsFlying() },
				{ "may_fly", player.MayFly() },
				{ "max_move_speed", player.maxMoveSpeed },
				{ "move_acceleration", player.moveAcceleration },
				{ "jump_force", player.jumpForce },
				{ "fly_speed", player.flySpeed },
				{ "sprint_multiplier", player.sprintMultiplier },
				{ "air_control", player.airControl }
			};

		[NoxPublic(NoxAccess.Method)]
		public void SetAbilities(string key, object value) {
			if (!GetAbilities().ContainsKey(key)) return;
			switch (key) {
				case "immobilized":
					player.useMovement = !(bool)value;
					break;
				case "crouching":
					player.SetCrouching((bool)value);
					break;
				case "sprinting":
					player.SetSprinting((bool)value);
					break;
				case "flying":
					if ((bool)value != player.IsFlying())
						player.ToggleFlying();
					break;
				case "may_fly":
					player.SetMayFly((bool)value);
					break;
				case "max_move_speed":
					player.maxMoveSpeed = (float)value;
					break;
				case "move_acceleration":
					player.moveAcceleration = (float)value;
					break;
				case "jump_force":
					player.jumpForce = (float)value;
					break;
				case "fly_speed":
					player.flySpeed = (float)value;
					break;
				case "sprint_multiplier":
					player.sprintMultiplier = (float)value;
					break;
				case "air_control":
					player.airControl = (float)value;
					break;
			}
		}

		[NoxPublic(NoxAccess.Method)]
		public Dictionary<ushort, Transform> GetParts()
			=> new() {
				{ PlayerRig.Base.ToIndex(), transform },
				{ PlayerRig.Head.ToIndex(), player.headCamera.transform }
			};

		private IPlayer _attachedPlayer;
		private IAvatar _attachedAvatar;

		[NoxPublic(NoxAccess.Method)]
		public void SetPlayer(IPlayer p) {
			_attachedPlayer = p;
			Client.CoreAPI.EventAPI.Emit("controller_set_player", this, _attachedPlayer);
			if (p == null) return;
			SynchronizeControllerFromPlayer();
		}

		public IAvatar GetAvatar()
			=> _attachedAvatar;

		public void SetAvatar(IAvatar avatar) {
			_attachedAvatar = avatar;
			if (_attachedAvatar == null) return;
			var root = _attachedAvatar.GetDescriptor()?.GetRoot();
			if (!root) {
				Logger.LogError("Avatar descriptor root is null, cannot set avatar.");
				return;
			}

			root.transform.SetParent(transform, false);
			root.transform.localPosition = Vector3.zero;
			root.transform.localRotation = Quaternion.identity;

			var parameterModule = _attachedAvatar?.GetDescriptor()
				?.GetModules<IParameterModule>()
				.FirstOrDefault();
			if (parameterModule == null) return;
			var parameters = parameterModule.GetParameters();
			foreach (var param in parameters) {
				var n = param.GetName();
				switch (n) {
					case "tracking/head/active":
						param.Set(true);
						break;
					case "tracking/left_hand/active":
						param.Set(false);
						break;
					case "tracking/right_hand/active":
						param.Set(false);
						break;
					case "tracking/left_foot/active":
						param.Set(false);
						break;
					case "tracking/right_foot/active":
						param.Set(false);
						break;
				}
			}
		}

		[NoxPublic(NoxAccess.Method)]
		public IPlayer GetPlayer()
			=> _attachedPlayer;

		private void Update() {
			SynchronizePlayerFromController();
			SynchronizeParametersAvatar();
		}

		private void LateUpdate()
			=> UpdateCamera();

		// ReSharper disable Unity.PerformanceAnalysis
		private void SynchronizePlayerFromController() {
			if (_attachedPlayer == null) return;
			foreach (var part in GetParts())
				_attachedPlayer.MovePart(
					part.Key,
					new NoxTransform(part.Value, part.Value.GetComponent<Rigidbody>())
				);
		}

		private void SynchronizeControllerFromPlayer() {
			if (_attachedPlayer == null) return;
			Logger.LogDebug("Synchronizing player from controller");
			transform.position = _attachedPlayer.GetPosition();
			transform.rotation = _attachedPlayer.GetRotation();
		}

		// ReSharper disable Unity.PerformanceAnalysis
		private void SynchronizeParametersAvatar() {
			var parameterModule = _attachedAvatar?.GetDescriptor()
				?.GetModules<IParameterModule>()
				.FirstOrDefault();
			if (parameterModule == null) return;
			var parameters = parameterModule.GetParameters();
			foreach (var param in parameters) {
				var n = param.GetName();
				switch (n) {
					case "grounded" or "Grounded": {
						var grounded = player.IsGrounded();
						var value    = (bool)param.Get();
						if (value == grounded) continue;
						param.Set(grounded);
						break;
					}
					case "VelocityX" or "velocity_x": {
						var velocity = player.body?.linearVelocity ?? Vector3.zero;
						var value    = (float)param.Get();
						if (Mathf.Approximately(value, velocity.x)) continue;
						param.Set(velocity.x);
						break;
					}
					case "VelocityY" or "velocity_y": {
						var velocity = player.body?.linearVelocity ?? Vector3.zero;
						var value    = (float)param.Get();
						if (Mathf.Approximately(value, velocity.y)) continue;
						param.Set(velocity.y);
						break;
					}
					case "VelocityZ" or "velocity_z": {
						var velocity = player.body?.linearVelocity ?? Vector3.zero;
						var value    = (float)param.Get();
						if (Mathf.Approximately(value, velocity.z)) continue;
						param.Set(velocity.z);
						break;
					}
					case "Velocity" or "velocity": {
						var velocity = player.body?.linearVelocity ?? Vector3.zero;
						var value    = (Vector3)param.Get();
						if (value == velocity) continue;
						param.Set(velocity);
						break;
					}
					case "tracking/head/rotation": {
						var cRot  = player.headCamera.transform.rotation;
						var value = (Quaternion)param.Get();
						if (Quaternion.Angle(value, cRot) < 0.001f) continue;
						param.Set(cRot);
						break;
					}
				}
			}
		}

		private void UpdateCamera() {
			var cameraModule = _attachedAvatar?.GetDescriptor()
				?.GetModules<ICameraModule>()
				.FirstOrDefault();

			if (cameraModule == null)
				return;

			var offset = cameraModule.GetOffset();
			var anchor = cameraModule.GetAnchor();
			anchor.GetPositionAndRotation(out var pos, out var rot);

			pos += anchor.TransformDirection(offset);

			player.headCamera.transform.position = pos;
		}
	}
}