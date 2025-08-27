using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Autohand;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.Avatars.Camera;
using Nox.Avatars.Parameters;
using Nox.CCK.Mods.Events;
using Nox.CCK.Players;
using Nox.CCK.Utils;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;
using Transform = UnityEngine.Transform;
using Nox.Controllers;
using Nox.Players;
using Nox.UI;
using Nox.Users;
using UnityEngine.EventSystems;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using NoxTransform = Nox.CCK.Utils.Transform;

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

			if (xr._attachedRuntimeAvatar == null)
				xr.SetupAvatar().Forget();

			xr._onUserUpdate = Client.CoreAPI.EventAPI.Subscribe("user_update", xr.OnUserUpdate);

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

		// Avatar management fields
		private IRuntimeAvatar          _attachedRuntimeAvatar;
		private IAvatarIdentifier       _avatarIdentifier;
		private CancellationTokenSource _avatarLoadingCts;
		private EventSubscription       _onUserUpdate;

		public void Dispose() {
			Client.CoreAPI.EventAPI.Unsubscribe(_onUserUpdate);
			_onUserUpdate = null;
			_avatarLoadingCts?.Cancel();
			_avatarLoadingCts?.Dispose();
			_avatarLoadingCts = null;
			_attachedRuntimeAvatar?.Dispose();
			_attachedRuntimeAvatar = null;
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
			SetAvatar(controller.GetAvatar(), controller.GetAvatarIdentifier());
			controller.SetAvatar(null);
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
				{ "flying", !player.useGrounding },
				{ "may_fly", mayFly },
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
					if (!player.useGrounding != (bool)value)
						player.ToggleFlying();
					break;
				case "may_fly":
					mayFly = (bool)value;
					if (!player.useGrounding && !mayFly)
						player.ToggleFlying();
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

		public IRuntimeAvatar GetAvatar()
			=> _attachedRuntimeAvatar;

		public IAvatarIdentifier GetAvatarIdentifier()
			=> _avatarIdentifier;


		public void SetAvatar(IRuntimeAvatar runtimeAvatar, IAvatarIdentifier identifier = null) {
			Logger.LogDebug("Setting avatar for XRController");
			if (runtimeAvatar == _attachedRuntimeAvatar) return;

			var old = _attachedRuntimeAvatar;
			_attachedRuntimeAvatar = runtimeAvatar;

			if (_attachedRuntimeAvatar == null) {
				Logger.LogWarning("Setting avatar to null, removing current avatar.");
				_attachedRuntimeAvatar = old;
				return;
			}

			var root = _attachedRuntimeAvatar.GetDescriptor().GetRoot();
			if (!root) {
				Logger.LogError("Avatar descriptor root is null, cannot set avatar.");
				_attachedRuntimeAvatar = old;
				return;
			}

			_avatarIdentifier = identifier;
			old?.Dispose();

			Logger.LogDebug($"Attaching avatar to {runtimeAvatar.GetDescriptor()}", runtimeAvatar.GetDescriptor().GetRoot());
			root.transform.SetParent(transform, false);
			root.transform.localPosition = Vector3.zero;
			root.transform.localRotation = Quaternion.identity;

			var parameterModule = _attachedRuntimeAvatar?.GetDescriptor()
				?.GetModules<IParameterModule>()
				.FirstOrDefault();
			if (parameterModule == null) {
				Logger.LogWarning("Avatar has no parameter module, cannot configure tracking parameters.");
				return;
			}

			var parameters = parameterModule.GetParameters();
			foreach (var param in parameters) {
				var n = param.GetName();
				switch (n) {
					case "tracking/head/active":
						param.Set(Client.Instance.HasHeadset());
						break;
					case "tracking/left_hand/active":
						param.Set(Client.Instance.HasHandLeft());
						break;
					case "tracking/right_hand/active":
						param.Set(Client.Instance.HasHandRight());
						break;
					case "tracking/left_foot/active":
						// param.Set(Client.Instance.HasFootLeft());
						param.Set(false);
						break;
					case "tracking/right_foot/active":
						// param.Set(Client.Instance.HasFootRight());
						param.Set(false);
						break;
				}
			}
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
			// Avatar parameter synchronization temporarily disabled due to missing APIs
			SynchronizeParametersAvatar();
		}

		// private void LateUpdate()
		// 	=> UpdateCamera();

		private void OnUserUpdate(EventData context) {
			if (!context.TryGet(0, out ICurrentUser user) || user == null || !IsCurrent()) return;
			LoadAvatarFromUser(user);
		}

		private void LoadAvatarFromUser(ICurrentUser user)
			=> LoadAvatar(Client.AvatarAPI.Make(user?.GetAvatarId())).Forget();

		private async UniTask LoadAvatar(IAvatarIdentifier identifier) {
			Logger.LogDebug($"Loading avatar for identifier {identifier?.ToString() ?? "null"}");
			if (identifier == null || !identifier.IsValid()) return;
			if (identifier.Equals(_avatarIdentifier)) return;

			_avatarLoadingCts?.Cancel();
			_avatarLoadingCts = new CancellationTokenSource();

			var asset = (await Client.AvatarAPI.SearchAssets(
						identifier.ToString(),
						Client.AvatarAPI.MakeAssetSearchRequest()
							.SetEngines(new[] { EngineExtensions.CurrentEngine.GetEngineName() })
							.SetPlatforms(new[] { PlatformExtensions.CurrentPlatform.GetPlatformName() })
							.SetLimit(1)
							.SetVersions(new[] { identifier.GetVersion() })
					)
					.AttachExternalCancellation(_avatarLoadingCts.Token)).GetAssets()
				.FirstOrDefault();
			if (_avatarLoadingCts.IsCancellationRequested) return;

			if (asset == null) {
				Logger.LogWarning($"Avatar asset not found for identifier {identifier.ToString()}");
				SetAvatar(await Client.AvatarAPI.LoadError(), identifier);
				return;
			}

			if (!Client.AvatarAPI.HasInCache(asset.GetHash())) {
				var download = Client.AvatarAPI.DownloadToCache(
					asset.GetUrl(),
					hash: asset.GetHash(),
					progress: p => Logger.LogDebug($"Downloading avatar {identifier.ToString()}: {p:P1}"),
					token: _avatarLoadingCts.Token
				);
				download.Start();
				await download.Wait();
				if (_avatarLoadingCts.IsCancellationRequested) return;
			}

			var avatar = await Client.AvatarAPI.LoadFromCache(
				asset.GetHash(),
				progress: p => Logger.LogDebug($"Loading avatar {identifier.ToString()}: {p:P1}"),
				token: _avatarLoadingCts.Token
			);
			if (_avatarLoadingCts.IsCancellationRequested) return;

			if (avatar == null) {
				Logger.LogError($"Failed to load avatar from cache for identifier {identifier.ToString()}");
				SetAvatar(await Client.AvatarAPI.LoadError(), identifier);
				return;
			}

			Logger.LogDebug($"Avatar loaded: {identifier.ToString()}");
			SetAvatar(avatar, identifier);
		}

		private async UniTask SetupAvatar() {
			if (_attachedRuntimeAvatar != null) {
				Logger.LogDebug("Avatar already set for XRController");
				return;
			}

			Logger.LogDebug("Creating avatar");

			var avatar = await Client.AvatarAPI.LoadLoading();
			if (avatar == null) {
				Logger.LogError("Failed to create avatar for XRController");
				return;
			}

			SetAvatar(avatar);

			LoadAvatarFromUser(Client.UserAPI.GetCurrent());
		}

		// ReSharper disable Unity.PerformanceAnalysis
		private void SynchronizeParametersAvatar() {
			var parameterModule = _attachedRuntimeAvatar?.GetDescriptor()
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
						var worldVelocity = player.body?.linearVelocity ?? Vector3.zero;
						var localVelocity = transform.InverseTransformDirection(worldVelocity);
						var value         = (float)param.Get();
						if (Mathf.Approximately(value, localVelocity.x)) continue;
						param.Set(localVelocity.x);
						break;
					}
					case "VelocityY" or "velocity_y": {
						var worldVelocity = player.body?.linearVelocity ?? Vector3.zero;
						var localVelocity = transform.InverseTransformDirection(worldVelocity);
						var value         = (float)param.Get();
						if (Mathf.Approximately(value, localVelocity.y)) continue;
						param.Set(localVelocity.y);
						break;
					}
					case "VelocityZ" or "velocity_z": {
						var worldVelocity = player.body?.linearVelocity ?? Vector3.zero;
						var localVelocity = transform.InverseTransformDirection(worldVelocity);
						var value         = (float)param.Get();
						if (Mathf.Approximately(value, localVelocity.z)) continue;
						param.Set(localVelocity.z);
						break;
					}
					case "Velocity" or "velocity": {
						var worldVelocity = player.body?.linearVelocity ?? Vector3.zero;
						var localVelocity = transform.InverseTransformDirection(worldVelocity);
						var value         = (Vector3)param.Get();
						if (value == localVelocity) continue;
						param.Set(localVelocity);
						break;
					}

					// Tracking de la tête - position et rotation
					case "tracking/head/active": {
						var active = Client.Instance.HasHeadset();
						var value  = (bool)param.Get();
						if (value == active) continue;
						param.Set(active);
						break;
					}
					case "tracking/head/position": {
						var cPos  = player.headCamera.transform.position;
						var value = (Vector3)param.Get();
						if (Vector3.Distance(value, cPos) < 0.001f) continue;
						param.Set(cPos);
						break;
					}
					case "tracking/head/rotation": {
						var cRot  = player.headCamera.transform.rotation;
						var value = (Quaternion)param.Get();
						if (Quaternion.Angle(value, cRot) < 0.001f) continue;
						param.Set(cRot);
						break;
					}

					// Tracking des mains - position et rotation
					case "tracking/left_hand/active": {
						var active = Client.Instance.HasHandLeft();
						var value  = (bool)param.Get();
						if (value == active) continue;
						param.Set(active);
						break;
					}
					case "tracking/left_hand/position": {
						var cPos  = player.handLeft.transform.position;
						var value = (Vector3)param.Get();
						if (Vector3.Distance(value, cPos) < 0.001f) continue;
						param.Set(cPos);
						break;
					}
					case "tracking/left_hand/rotation": {
						var cRot  = player.handLeft.transform.rotation;
						var value = (Quaternion)param.Get();
						if (Quaternion.Angle(value, cRot) < 0.001f) continue;
						param.Set(cRot);
						break;
					}

					case "tracking/right_hand/active": {
						var active = Client.Instance.HasHandRight();
						var value  = (bool)param.Get();
						if (value == active) continue;
						param.Set(active);
						break;
					}
					case "tracking/right_hand/position": {
						var cPos  = player.handRight.transform.position;
						var value = (Vector3)param.Get();
						if (Vector3.Distance(value, cPos) < 0.001f) continue;
						param.Set(cPos);
						break;
					}
					case "tracking/right_hand/rotation": {
						var cRot  = player.handRight.transform.rotation;
						var value = (Quaternion)param.Get();
						if (Quaternion.Angle(value, cRot) < 0.001f) continue;
						param.Set(cRot);
						break;
					}

					// ...existing code for other tracking parameters...
				}
			}
		}

		// private void UpdateCamera() {
		// 	var cameraModule = _attachedRuntimeAvatar?.GetDescriptor()
		// 		?.GetModules<ICameraModule>()
		// 		.FirstOrDefault();
		//
		// 	if (cameraModule == null)
		// 		return;
		//
		// 	var offset = cameraModule.GetOffset();
		// 	var anchor = cameraModule.GetAnchor();
		// 	anchor.GetPositionAndRotation(out var pos, out var _);
		//
		// 	pos += anchor.TransformDirection(offset);
		//
		// 	player.headCamera.transform.position = pos;
		// }

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
	}
}