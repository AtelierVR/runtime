using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.Avatars.Camera;
using Nox.Avatars.Controllers;
using Nox.Avatars.Parameters;
using Nox.Avatars.Players;
using Nox.CCK.Mods.Events;
using Nox.CCK.Players;
using Nox.CCK.Utils;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;
using Transform = UnityEngine.Transform;
using Nox.Controllers;
using Nox.Players;
using Nox.Users;
using UnityEngine.EventSystems;
using NoxTransform = Nox.CCK.Utils.Transform;

namespace api.nox.desktop {
	public class DesktopController : MonoBehaviour, IController, IControllerAvatar, INoxObject {
		private static int DefaultPriority
			=> Config.Load().Get("settings.controller.desktop_priority", IController.DefaultPriority);

		private const string DefaultId = "desktop";

		[Header("Zoom Settings")]
		[SerializeField]
		private float zoomSpeed = 2f;

		[SerializeField]
		private float minZoom = 2f;

		[SerializeField]
		private float maxZoom = 60f;

		private float currentZoom = 60f;

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
		internal static async UniTask<bool> Remove() {
			if (!IsCurrent()) return false;
			return await ControllerAPI.SetCurrent(null);
		}

		/// <summary>
		/// Create the Desktop proxy if it is not already created.
		/// </summary>
		/// <returns></returns>
		internal static async UniTask<bool> Make() {
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

			if (!await ControllerAPI.SetCurrent(desktop)) {
				Logger.LogError("Failed to set Desktop proxy as current");
				Destroy(instance);
				return false;
			}

			if (desktop._attachedRuntimeAvatar == null)
				desktop.SetupAvatar().Forget();

			desktop._onUserUpdate = Client.CoreAPI.EventAPI.Subscribe("user_update", desktop.OnUserUpdate);

			EventSystem.current     = desktop.eventSystem;
			desktop.gameObject.name = $"[{desktop.GetType().Name}_{desktop.GetInstanceID()}]";
			DontDestroyOnLoad(desktop);
			return true;
		}

		private void OnUserUpdate(EventData context) {
			if (!context.TryGet(0, out ICurrentUser user) || user == null || !IsCurrent()) return;
			LoadAvatarFromUser(user);
		}

		private void LoadAvatarFromUser(ICurrentUser user)
			=> SetAvatar(Client.AvatarAPI.Make(user?.GetAvatarId())).Forget();

		public async UniTask<IRuntimeAvatar> SetAvatar(IAvatarIdentifier identifier, Action<string, float> onProgress = null) {
			Logger.LogDebug($"Loading avatar for identifier {identifier?.ToString() ?? "null"}");

			var playerAvatar = _attachedPlayer as ILocalPlayerAvatar;

			if (identifier == null || !identifier.IsValid()) {
				if (playerAvatar != null)
					await playerAvatar.SendAvatarFailed("Invalid avatar identifier.");
				return null;
			}

			if (identifier.Equals(_attachedRuntimeAvatar?.GetIdentifier())) {
				if (playerAvatar != null)
					await playerAvatar.SendAvatarReady();
				return _attachedRuntimeAvatar;
			}

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

			if (_avatarLoadingCts.IsCancellationRequested)
				return null;

			if (asset == null) {
				Logger.LogWarning($"Avatar asset not found for identifier {identifier.ToString()}");
				var err = await Client.AvatarAPI.LoadError();
				err.SetIdentifier(identifier);
				await SetAvatar(err);
				if (playerAvatar != null)
					await playerAvatar.SendAvatarFailed("Avatar asset not found.");
				return null;
			}

			if (!Client.AvatarAPI.HasInCache(asset.GetHash())) {
				var download = Client.AvatarAPI.DownloadToCache(
					asset.GetUrl(),
					hash: asset.GetHash(),
					progress: p => onProgress?.Invoke($"Downloading avatar {identifier.ToString()}", p),
					token: _avatarLoadingCts.Token
				);
				download.Start();
				await download.Wait();
				if (_avatarLoadingCts.IsCancellationRequested)
					return null;
			}

			var avatar = await Client.AvatarAPI.LoadFromCache(
				asset.GetHash(),
				progress:  p => onProgress?.Invoke($"Loading avatar{identifier.ToString()}", p),
				token: _avatarLoadingCts.Token
			);
			if (_avatarLoadingCts.IsCancellationRequested)
				return null;

			if (avatar == null) {
				Logger.LogError($"Failed to load avatar from cache for identifier {identifier.ToString()}");
				var err = await Client.AvatarAPI.LoadError();
				err.SetIdentifier(identifier);
				await SetAvatar(err);
				if (playerAvatar != null)
					await playerAvatar.SendAvatarFailed("Failed to load avatar from cache.");
				return null;
			}

			Logger.LogDebug($"Avatar loaded: {identifier.ToString()}");
			avatar.SetIdentifier(identifier);
			await SetAvatar(avatar);
			if (playerAvatar != null)
				await playerAvatar.SendAvatarReady();
			return avatar;
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
			Client.CoreAPI.EventAPI.Unsubscribe(_onUserUpdate);
			_onUserUpdate = null;
			_avatarLoadingCts?.Cancel();
			_avatarLoadingCts?.Dispose();
			_avatarLoadingCts = null;
			_attachedRuntimeAvatar?.Dispose();
			_attachedRuntimeAvatar = null;
			Destroy(gameObject);
		}

		private async UniTask SetupAvatar() {
			if (_attachedRuntimeAvatar != null) {
				Logger.LogDebug("Avatar already set for DesktopController");
				return;
			}

			Logger.LogDebug("Creating avatar");

			var avatar = await Client.AvatarAPI.LoadLoading();
			if (avatar == null) {
				Logger.LogError("Failed to create avatar for DesktopController");
				return;
			}

			await SetAvatar(avatar);

			LoadAvatarFromUser(Client.UserAPI.GetCurrent());
		}

		[NoxPublic(NoxAccess.Method)]
		public Camera GetCamera()
			=> player.headCamera;

		[NoxPublic(NoxAccess.Method)]
		public Collider GetCollider()
			=> player.bodyCollider;

		public async UniTask Restore(IController controller) {
			foreach (var ability in controller.GetAbilities())
				SetAbilities(ability.Key, ability.Value);

			if (controller is IControllerAvatar ca) {
				await SetAvatar(ca.GetAvatar());
				ca.SetAvatar((IRuntimeAvatar)null);
			}
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

		private IPlayer                 _attachedPlayer;
		private IRuntimeAvatar          _attachedRuntimeAvatar;
		private CancellationTokenSource _avatarLoadingCts;
		private EventSubscription       _onUserUpdate;

		[NoxPublic(NoxAccess.Method)]
		public void SetPlayer(IPlayer p) {
			_attachedPlayer = p;
			Client.CoreAPI.EventAPI.Emit("controller_set_player", this, _attachedPlayer);
			if (p == null) return;
			SynchronizeControllerFromPlayer();
		}

		public IRuntimeAvatar GetAvatar()
			=> _attachedRuntimeAvatar;

		public async UniTask<bool> SetAvatar(IRuntimeAvatar runtimeAvatar) {
			Logger.LogDebug("Setting avatar for DesktopController");
			if (runtimeAvatar == _attachedRuntimeAvatar)
				return true;

			var old = _attachedRuntimeAvatar;
			_attachedRuntimeAvatar = runtimeAvatar;

			if (_attachedRuntimeAvatar == null) {
				Logger.LogWarning("Setting avatar to null, removing current avatar.");
				_attachedRuntimeAvatar = old;
				return false;
			}

			var root = _attachedRuntimeAvatar.GetDescriptor().GetAnchor();
			if (!root) {
				Logger.LogError("Avatar descriptor root is null, cannot set avatar.");
				_attachedRuntimeAvatar = old;
				return false;
			}

			if (old != null)
				await old.Dispose();

			Logger.LogDebug($"Attaching avatar to {runtimeAvatar.GetDescriptor()}", runtimeAvatar.GetDescriptor().GetAnchor());
			root.transform.SetParent(transform, false);
			root.transform.localPosition = Vector3.zero;
			root.transform.localRotation = Quaternion.identity;

			var parameterModule = _attachedRuntimeAvatar?.GetDescriptor()
				?.GetModules<IParameterModule>()
				.FirstOrDefault();

			if (parameterModule == null) {
				Logger.LogWarning("Avatar has no parameter module, cannot configure tracking parameters.");
				return true;
			}

			var parameters = parameterModule.GetParameters();
			foreach (var param in parameters) {
				var n = param.GetName();
				switch (n) {
					case "tracking/head/active":
						param.Set(true);
						break;
					case "tracking/left_hand/active":
					case "tracking/right_hand/active":
					case "tracking/left_foot/active":
					case "tracking/right_foot/active":
						param.Set(false);
						break;
				}
			}

			root.SetActive(true);

			return true;
		}

		[NoxPublic(NoxAccess.Method)]
		public IPlayer GetPlayer()
			=> _attachedPlayer;

		private void Update() {
			HandleZoomInput();
			SynchronizePlayerFromController();
			SynchronizeParametersAvatar();
		}

		private void HandleZoomInput() {
			// Vérifier si la souris n'est pas sur l'UI
			if (EventSystem.current && EventSystem.current.IsPointerOverGameObject())
				return;

			// Gérer le zoom avec la molette de la souris
			var scrollInput = Input.GetAxis("Mouse ScrollWheel");
			if (!(Mathf.Abs(scrollInput) > 0.01f)) return;
			// Calculer le nouveau zoom
			currentZoom -= scrollInput * zoomSpeed * 10f;
			currentZoom =  Mathf.Clamp(currentZoom, minZoom, maxZoom);

			// Appliquer le zoom à la caméra
			if (player?.headCamera)
				player.headCamera.fieldOfView = currentZoom;
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
			Logger.LogDebug($"Synchronizing controller from player at {_attachedPlayer.GetPosition()} with rotation {_attachedPlayer.GetRotation()}");
			var part = GetParts().FirstOrDefault(p => p.Key == PlayerRig.Base.ToIndex());
			player.SetPosition(_attachedPlayer.GetPosition());
			part.Value.rotation = _attachedPlayer.GetRotation();
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
			var cameraModule = _attachedRuntimeAvatar?.GetDescriptor()
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