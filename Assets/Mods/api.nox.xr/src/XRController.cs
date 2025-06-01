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

namespace api.nox.xr {
	public class XRProxy : MonoBehaviour, IController, INoxObject {
		private static int DefaultPriority
			=> ClientXR.IsReady() && ClientXR.Instance.HasHeadset()
				? Config.Load().Get("settings.xr.proxy_priority", ProxyAPIDefaultPriority + 1)
				: ProxyAPIDefaultPriority - 1;

		private const string DefaultId = "nox.xr";

		/// <summary>
		/// Get the proxy mod API.
		/// </summary>
		private static INoxObject PlayerAPI
			=> ClientXR.CoreAPI
				.ModAPI.GetMod("player")
				?.GetMains()
				.FirstOrDefault();

		private static int ProxyAPIDefaultPriority
			=> PlayerAPI?.CallMethod<int>("GetDefaultPriority") ?? 1;

		/// <summary>
		/// Check if the current proxy is better than XR proxy.
		/// </summary>
		/// <returns></returns>
		private static bool IsBetterThanCurrent() {
			var proxy = PlayerAPI.CallMethod("GetProxy");
			return proxy                                == null
				|| proxy.CallMethod<int>("GetPriority") < DefaultPriority
				|| proxy.CallMethod<string>("GetId")    == DefaultId;
		}

		/// <summary>
		/// Check if the current proxy is the XR proxy.
		/// </summary>
		/// <returns></returns>
		private static bool IsCurrent() {
			var proxy = PlayerAPI.CallMethod("GetProxy");
			return proxy                             != null
				&& proxy.CallMethod<string>("GetId") == DefaultId;
		}

		/// <summary>
		/// Remove the current proxy if it is the XR proxy.
		/// </summary>
		internal static bool Remove() {
			if (!IsCurrent()) return false;
			PlayerAPI.InvokeMethod("SetProxy", null);
			return true;
		}

		/// <summary>
		/// Create the XR proxy if it is not already created.
		/// </summary>
		/// <returns></returns>
		internal static bool Make() {
			if (!IsBetterThanCurrent()) return false;

			var prefab = ClientXR.CoreAPI.AssetAPI.GetAsset<GameObject>("proxy.prefab");
			if (!prefab) {
				Logger.LogError("Failed to load desktop proxy prefab");
				return false;
			}

			var instance = Instantiate(prefab);
			var xr       = instance.GetComponent<XRProxy>();

			if (!xr) {
				Logger.LogError("Failed to get desktop proxy component");
				Destroy(instance);
				return false;
			}

			PlayerAPI.InvokeMethod("SetProxy", xr);
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

		[NoxPublic(NoxAccess.Method)]
		public Camera GetCamera()
			=> player.headCamera;

		[NoxPublic(NoxAccess.Method)]
		public Collider GetCollider()
			=> player.bodyCollider;

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