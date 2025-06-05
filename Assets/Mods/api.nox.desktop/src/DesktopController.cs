using System.Collections.Generic;
using System.Linq;
using Nox.CCK.Development;
using Nox.CCK.Players;
using Nox.CCK.Utils;
using Nox.Controllers;
using Nox.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using Gizmos = Nox.CCK.Development.Gizmos;
using Logger = Nox.CCK.Utils.Logger;
using Transform = UnityEngine.Transform;

namespace api.nox.desktop {
	[Gizmos("desktop.controller")]
	public class DesktopController : MonoBehaviour, IController, INoxObject {
		private static int DefaultPriority
			=> Config.Load().Get("settings.controller.desktop_priority", IController.DefaultPriority);

		private const string DefaultId = "desktop";

		private static bool IsBetterThanCurrent() {
			var proxy = Client.ControllerAPI.GetCurrent();
			return proxy               == null
				|| proxy.GetPriority() < DefaultPriority
				|| proxy.GetId()       == DefaultId;
		}

		internal static bool Make() {
			if (!IsBetterThanCurrent()) return false;

			var prefab = Client.CoreAPI.AssetAPI.GetAsset<GameObject>("controller.prefab");
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
			desktop.menu = Client.UiAPI?.Make(desktop.menuContainer);
			if (desktop.menu == null) {
				Logger.LogError("Failed to create desktop proxy menu");
				Destroy(instance);
				return false;
			}

			desktop.menu.SetActive(false);

			if (!Client.ControllerAPI.SetCurrent(desktop)) {
				Logger.LogError("Failed to set desktop proxy as current");
				Destroy(instance);
				return false;
			}

			desktop._mouse          = InputSystem.GetDevice<Mouse>();
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

		[NoxPublic(NoxAccess.Method)]
		public Dictionary<ushort, Transform> GetParts()
			=> new() {
				{ PlayerRig.Base.ToIndex(), transform },
				{ PlayerRig.Head.ToIndex(), viewCamera.transform }
			};

		[HideInInspector] public Vector3 inputMovement = Vector3.zero;
		private                  Mouse   _mouse;

		public void Start() {
			LockCursor = true;
			Keybindings.KeyEvent.AddListener(OnKeyEvent);
		}

		private static bool LockCursor {
			get => Cursor.lockState == CursorLockMode.Locked;
			set {
				Cursor.lockState = value ? CursorLockMode.Locked : CursorLockMode.None;
				Cursor.visible   = !value;
			}
		}

		public CharacterController bodyController;
		public CapsuleCollider     bodyCollider;
		public Camera              viewCamera;
		public Transform           viewOffset;
		public Vector3             velocity = Vector3.zero;
		public RectTransform       menuContainer;
		public IMenu               menu;

		private readonly Dictionary<string, object> _abilities = new() {
			{ "may_fly", true }
		};

		[NoxPublic(NoxAccess.Method)]
		public Dictionary<string, object> GetAbilities()
			=> new(_abilities) {
				["walk_force"]           = WalkAcceleration,
				["sprint_force"]         = SprintAcceleration,
				["jump_force"]           = JumpAcceleration,
				["gravity_acceleration"] = GravityAcceleration,
				["friction"]             = Friction,
				["slipperiness"]         = Slipperiness,
				["grounded"]             = IsGrounded,
				["height"]               = Height,
				["flying"]               = IsFlying,
				["immobilized"]          = IsImmobilized
			};

		[NoxPublic(NoxAccess.Method)]
		public void SetAbilities(string key, object value) {
			switch (key) {
				case "immobilized" when value is bool b:
					_abilities["immobilized"] = b;
					break;
				case "flying" when value is bool b:
					IsFlying = b;
					break;
				case "walk_force" when value is float fw:
					WalkAcceleration = fw;
					break;
				case "sprint_force" when value is float fs:
					SprintAcceleration = fs;
					break;
				case "jump_force" when value is float fj:
					JumpAcceleration = fj;
					break;
				case "height" when value is float fh:
					Height = fh;
					break;
				default:
					if (_abilities.ContainsKey(key) && _abilities[key].GetType() == value.GetType())
						_abilities[key] = value;
					else _abilities.TryAdd(key, value);
					break;
			}
		}

		private float WalkAcceleration {
			get => _abilities.ContainsKey("walk_force") && _abilities["walk_force"] is float f ? f : 4.317f;
			set => _abilities["walk_force"] = value;
		}

		private float SprintAcceleration {
			get => _abilities.ContainsKey("sprint_force") && _abilities["sprint_force"] is float f ? f : WalkAcceleration * 1.3f;
			set => _abilities["sprint_force"] = value;
		}

		private float JumpAcceleration {
			get => _abilities.ContainsKey("jump_force") && _abilities["jump_force"] is float f ? f : 5f;
			set => _abilities["jump_force"] = value;
		}

		private float GravityAcceleration {
			get => _abilities.ContainsKey("gravity_acceleration") && _abilities["gravity_acceleration"] is float f ? f : 9.81f;
			set => _abilities["gravity_acceleration"] = value;
		}

		private bool IsFlying {
			get => _abilities.ContainsKey("flying") && _abilities["flying"] is true;
			set => _abilities["flying"] = value;
		}

		private float Friction {
			get => _abilities.ContainsKey("friction") && _abilities["friction"] is float f ? f : 0.98f;
			set => _abilities["friction"] = value;
		}

		private float Slipperiness {
			get => _abilities.ContainsKey("slipperiness") && _abilities["slipperiness"] is float f ? f : 0.5f;
			set => _abilities["slipperiness"] = value;
		}

		private bool IsImmobilized {
			get => _abilities.ContainsKey("immobilized") && _abilities["immobilized"] is true;
			set => _abilities["immobilized"] = value;
		}


		internal float Height {
			get => bodyCollider.height - bodyCollider.radius;
			set {
				value += bodyCollider.radius;

				bodyCollider.height   = value;
				bodyController.height = value;
				bodyCollider.center   = new Vector3(0, value * .5f, 0);

				var vPos = viewOffset.localPosition;
				vPos.y                   = value - bodyCollider.radius;
				viewOffset.localPosition = vPos;

				bodyController.height = bodyCollider.height;
				bodyController.center = bodyCollider.center;
				bodyController.radius = bodyCollider.radius;
			}
		}


		private bool IsGrounded
			=> bodyController.isGrounded;

		private void OnValidate() {
			Height = Height;
		}

		private void Update() {
			HandleMovement();
			HandleLook();
		}

		private void HandleMovement() {
			var immobilized = IsImmobilized;

			var acceleration = !immobilized
				? Keybindings.IsPressed("sprint")
					? SprintAcceleration
					: WalkAcceleration
				: 0f;

			var move = Keybindings.GetMovement().normalized
				* acceleration;

			velocity += transform.forward * move.y + transform.right * move.x;

			var grounded = IsGrounded;

			if (!immobilized && grounded && Keybindings.IsPressed("jump"))
				velocity.y = JumpAcceleration;

			if (!grounded)
				velocity.y -= GravityAcceleration * Time.deltaTime;

			Move(velocity);
			velocity.y *= Friction;
			velocity.x *= Slipperiness;
			velocity.z *= Slipperiness;
		}

		private void Move(Vector3 delta) {
			bodyController.Move(delta * Time.deltaTime);
		}

		private static float MouseSensitivity
			=> Config.Load().Get("settings.control.mouse_sensitivity", 100f);

		private static bool InvertMouse
			=> Config.Load().Get("settings.control.invert_mouse", false);

		private void HandleLook() {
			if (!LockCursor || _mouse == null) return;
			var look      = _mouse.delta.ReadValue();
			var lookDelta = (InvertMouse ? -1 : 1) * Time.deltaTime * MouseSensitivity * look;

			// rotate up-down with camera
			var cameraRotation = viewCamera.transform.localEulerAngles;
			cameraRotation.x -= lookDelta.y;

			// between 90,0 and 270,360 
			cameraRotation.x = cameraRotation.x > 180f
				? Mathf.Clamp(cameraRotation.x, 270f, 360f)
				: Mathf.Clamp(cameraRotation.x, -90f, 90f);
			viewCamera.transform.localEulerAngles = cameraRotation;

			// rotate left-right with player
			transform.Rotate(Vector3.up, lookDelta.x);
		}

		public void Restore(IController controller) {
			foreach (var ability in controller.GetAbilities())
				SetAbilities(ability.Key, ability.Value);
		}

		public void Dispose() {
			Keybindings.KeyEvent.RemoveListener(OnKeyEvent);
			Destroy(gameObject);
		}

		private void OnKeyEvent(string key, float v, float o) {
			if (key != "main" || !(o < 0.1f) || !(v > o)) return;
			LockCursor = !LockCursor;
			menu?.SetActive(!LockCursor);
		}

		[NoxPublic(NoxAccess.Method)]
		public Camera GetCamera()
			=> viewCamera;

		[NoxPublic(NoxAccess.Method)]
		public Collider GetCollider()
			=> bodyCollider;

		private void OnDrawGizmos() {
			if (!bodyCollider || !viewCamera) return;

			// make a line of raycast ground
			Gizmos.color = Color.red;
			var ray = new Ray(viewCamera.transform.position, Vector3.down);
			if (Physics.Raycast(ray, out var hit, 100f)) {
				Gizmos.DrawLine(ray.origin, hit.point);
				Gizmos.DrawSphere(hit.point, 0.1f);
			} else Gizmos.DrawLine(ray.origin, ray.origin + ray.direction * 100f);

			// make a capsule 
			Gizmos.color = Color.gray;
			Gizmos.DrawWireCube(
				bodyCollider.bounds.center,
				new Vector3(bodyCollider.bounds.size.x, bodyCollider.bounds.size.y, bodyCollider.bounds.size.z)
			);
			// place view line
			Gizmos.color = Color.yellow;
			Gizmos.DrawLine(
				viewCamera.transform.position,
				viewCamera.transform.position + viewCamera.transform.forward * 1f
			);
			Gizmos.DrawSphere(
				viewCamera.transform.position,
				0.1f
			);
		}
	}
}