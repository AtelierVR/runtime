using System;
using System.Collections.Generic;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Players;
using Nox.CCK.Utils;
using UnityEngine;
using UnityEngine.Events;
using Transform = UnityEngine.Transform;

namespace api.nox.controller {
	public class ControllerSystem : MainModInitializer {
		internal static MainModCoreAPI   CoreAPI;
		internal static ControllerSystem Instance;

		private                INoxObject             _controller;
		public static readonly UnityEvent<INoxObject> OnControllerAdded   = new();
		public static readonly UnityEvent<INoxObject> OnControllerRemoved = new();

		[NoxPublic(NoxAccess.Method)]
		public INoxObject GetController()
			=> _controller;

		[NoxPublic(NoxAccess.Method)]
		public void SetController(INoxObject controller) {
			if (_controller == controller)
				return;

			if (_controller != null) {
				OnControllerRemoved.Invoke(_controller);
				if (_controller.HasMethod("Dispose"))
					_controller.InvokeMethod("Dispose");
			}

			_controller = controller;
			OnControllerAdded.Invoke(_controller);
		}
		
		public void OnInitializeMain(MainModCoreAPI api) {
			CoreAPI  = api;
			Instance = this;
			SetController(null);
		}

		public void OnDisposeMain() {
			SetController(null);
			CoreAPI  = null;
			Instance = null;
		}

		/// <summary>
		/// Identifier of the controller.
		/// Is for distinguishing between different controllers.
		/// </summary>
		[NoxPublic(NoxAccess.Read)]
		public string Id
			=> _controller?.GetField<string>("Id") ?? string.Empty;

		/// <summary>
		/// Priority of the controller.
		/// Is used to determine which controller should be used between multiple controllers.
		/// For example, a controller (xr) with a higher priority should be used over a controller (desktop) with a lower priority.
		/// </summary>
		[NoxPublic(NoxAccess.Read)]
		public int Priority
			=> _controller?.GetField<int>("Priority") ?? 0;

		/// <summary>
		/// When the controller have disabled movement of the player, the value is true.
		/// </summary>
		[NoxPublic(NoxAccess.Field)]
		public bool IsFrozen {
			get => _controller?.GetField<bool>("IsFrozen") ?? false;
			set => _controller?.SetField("IsFrozen", value);
		}

		/// <summary>
		/// When the player can move in air, the value is true.
		/// </summary>
		[NoxPublic(NoxAccess.Field)]
		public bool CanFly {
			get => _controller?.GetField<bool>("CanFly") ?? false;
			set => _controller?.SetField("CanFly", value);
		}

		/// <summary>
		/// When the player is currently flying, the value is true.
		/// </summary>
		[NoxPublic(NoxAccess.Field)]
		public bool IsFlying {
			get => _controller?.GetField<bool>("IsFlying") ?? false;
			set => _controller?.SetField("IsFlying", value);
		}

		/// <summary>
		/// When the player is on the ground, the value is true.
		/// </summary>
		[NoxPublic(NoxAccess.Read)]
		public bool IsGrounded
			=> _controller?.GetField<bool>("IsGrounded") ?? false;

		/// <summary>
		/// When the player is crouching, the value is true.
		/// On set, the value have effect as toggle the crouch state.
		/// </summary>
		[NoxPublic(NoxAccess.Field)]
		public bool IsCrouching {
			get => _controller?.GetField<bool>("IsCrouching") ?? false;
			set => _controller?.SetField("IsCrouching", value);
		}

		/// <summary>
		/// Get transform of a part of the rig.
		/// </summary>
		/// <param name="rig"></param>
		/// <returns></returns>
		public Transform GetPart(HumanBodyBones rig)
			=> GetPart(rig.ToPlayerRig().ToIndex());
		
		public Transform GetPart(PlayerRig rig)
			=> _controller.CallMethod<Transform>("GetPart", rig.ToIndex());

		/// <summary>
		/// Get transform of a part of the rig, but using the rig identifier.
		/// Note: with this, you can add your own rig parts (like ears, tail, more bones, etc).
		/// </summary>
		/// <param name="rig"></param>
		/// <returns></returns>
		public Transform GetPart(ushort rig) {
			if (_controller.HasMethod("GetPart"))
				return _controller.CallMethod<Transform>("GetPart", rig);
			return GetParts().GetValueOrDefault(rig);
		}

		/// <summary>
		/// Get all parts of the rig.
		/// </summary>
		/// <returns></returns>
		public Dictionary<ushort, Transform> GetParts()
			=> _controller.CallMethod<Dictionary<ushort, Transform>>("GetParts");

		/// <summary>
		/// Is the default controller priority.
		/// Is exist for other mod need to make sure that can overriding the default controller.
		/// </summary>
		/// <returns></returns>
		[NoxPublic(NoxAccess.Method)]
		public int GetDefaultPriority()
			=> DesktopController.DefaultPriority;
	}
}