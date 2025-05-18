using System;
using System.Collections.Generic;
using System.Linq;
using Nox.CCK.Avatars;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Players;
using Nox.CCK.Utils;
using UnityEngine;
using UnityEngine.InputSystem;
using Logger = Nox.CCK.Utils.Logger;
using Transform = UnityEngine.Transform;

namespace api.nox.player {
	public class DesktopProxy : MonoBehaviour, INoxObject {
		internal static int DefaultPriority
			=> Config.Load().Get("settings.player.default_proxy_priority", 1);

		private const string DefaultId = "nox.desktop";

		internal static bool IsBetterThanCurrent() {
			var proxy = PlayerSystem.Instance.GetProxy();
			return proxy                           == null
				|| PlayerSystem.GetPriority(proxy) < DefaultPriority
				|| PlayerSystem.GetId(proxy)       == DefaultId;
		}

		internal static bool Make() {
			if (IsBetterThanCurrent()) return false;

			var prefab = PlayerSystem.CoreAPI.AssetAPI.GetAsset<GameObject>("proxy.prefab");
			if (!prefab) {
				Logger.LogError("Failed to load desktop proxy prefab");
				return false;
			}

			var instance = Instantiate(prefab);
			var desktop  = instance.GetComponent<DesktopProxy>();

			if (!desktop) {
				Logger.LogError("Failed to get desktop proxy component");
				Destroy(instance);
				return false;
			}

			PlayerSystem.Instance.SetProxy(desktop);
			desktop.gameObject.name = $"[{desktop.GetType()}_{desktop.GetInstanceID()}]";
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
				{ PlayerRig.Base.ToIndex(), transform }
			};

		private readonly string[] _keys = {
			"forward",
			"backward",
			"left",
			"right",
			"jump",
			"crouch",
			"sprint",
			"menu"
		};

		private EventSubscription _keyBindingRemoved;

		private static MainModInitializer Keybinding
			=> PlayerSystem
				.CoreAPI.ModAPI
				.GetMod("keybinding")
				?.GetMains()
				.FirstOrDefault();


		private          Vector3 _inputMovement = Vector3.zero;
		private readonly float[] _movementKeys  = new float[6];
		private          Mouse   _mouse;

		public void Start() {
			Logger.LogDebug("DesktopController.Start");
			LockCursor = true;
			_keyBindingRemoved = PlayerSystem
				.CoreAPI.EventAPI
				.Subscribe("key_binding_removed", OnKeyBindingRemoved);
			Rebind();
			_mouse = InputSystem.GetDevice<Mouse>();
		}

		private void OnKeyBindingRemoved(EventData args)
			=> Rebind();

		private void Rebind() {
			foreach (var key in _keys) {
				if (!Keybinding.CallMethod<bool>("HasKeyBinding", $"generic.movement.{key}"))
					Keybinding.InvokeMethod(
						"AddKeyBinding",
						$"generic.movement.{key}",
						new InputAction(
							$"generic.movement.{key}",
							InputActionType.Button,
							key switch {
								"jump"     => "<Keyboard>/space",
								"crouch"   => "<Keyboard>/leftCtrl",
								"forward"  => "<Keyboard>/w",
								"backward" => "<Keyboard>/s",
								"left"     => "<Keyboard>/a",
								"right"    => "<Keyboard>/d",
								"sprint"   => "<Keyboard>/leftShift",
								"menu"     => "<Keyboard>/tab",
								_          => throw new ArgumentOutOfRangeException()
							}
						),
						"generic.movement"
					);
			}


			foreach (var key in _keys) {
				var action = Keybinding.CallMethod("GetKeyBinding", $"generic.movement.{key}")
					.GetField<InputAction>("Action");
				if (action == null) {
					Logger.LogWarning($"Initialized keybinding for {key} not found");
					continue;
				}

				if (new[] { "forward", "backward", "left", "right", "jump", "crouch" }.Contains(key)) {
					action.performed += ctx => OnMoveKey(key, ctx.ReadValue<float>());
					action.canceled  += _ => OnMoveKey(key, 0);
				} else if (key == "menu")
					action.performed += ctx => { };
				//ToggleMenu;
			}
		}
		
		private static bool LockCursor
		{
			get => Cursor.lockState == CursorLockMode.Locked;
			set
			{
				Cursor.lockState = value ? CursorLockMode.Locked : CursorLockMode.None;
				Cursor.visible   = !value;
			}
		}


		private void OnMoveKey(string key, float value) {
			Logger.Log($"Key {key} pressed with value {value}");
			switch (key) {
				case "forward":
					_movementKeys[0] = value;
					break;
				case "backward":
					_movementKeys[1] = value;
					break;
				case "left":
					_movementKeys[2] = value;
					break;
				case "right":
					_movementKeys[3] = value;
					break;
				case "jump":
					_movementKeys[4] = value;
					break;
				case "crouch":
					_movementKeys[5] = value;
					break;
			}

			_inputMovement = new Vector3(
				_movementKeys[3] - _movementKeys[2],
				_movementKeys[4] - _movementKeys[5],
				_movementKeys[0] - _movementKeys[1]
			).normalized;
		}
	}
}