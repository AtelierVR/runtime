using System;
using System.Collections.Generic;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Players;
using Nox.CCK.Utils;
using UnityEngine;
using UnityEngine.Events;
using Logger = Nox.CCK.Utils.Logger;
using Transform = UnityEngine.Transform;

namespace api.nox.player {
	public class PlayerSystem : MainModInitializer {
		internal static MainModCoreAPI CoreAPI;
		internal static PlayerSystem   Instance;

		private                INoxObject             _proxy;
		public static readonly UnityEvent<INoxObject> OnProxyAdded   = new();
		public static readonly UnityEvent<INoxObject> OnProxyRemoved = new();

		[NoxPublic(NoxAccess.Method)]
		public INoxObject GetProxy()
			=> _proxy;

		[NoxPublic(NoxAccess.Method)]
		public void SetProxy(INoxObject proxy) {
			if (_automaticReplaceNullProxy && proxy == null) {
				if (!DesktopProxy.Make())
					Logger.LogError("Failed to replace null proxy.");
				return;
			}

			if (_proxy == proxy)
				return;

			if (_proxy != null) {
				OnProxyRemoved.Invoke(_proxy);
				if (_proxy.HasMethod("Dispose"))
					_proxy.InvokeMethod("Dispose");
			}

			_proxy = proxy;
			OnProxyAdded.Invoke(_proxy);
		}

		public void OnInitializeMain(MainModCoreAPI api) {
			CoreAPI                    = api;
			Instance                   = this;
			_automaticReplaceNullProxy = Application.isPlaying;
			SetProxy(null);
		}

		private bool _automaticReplaceNullProxy = true;

		public void OnDisposeMain() {
			_automaticReplaceNullProxy = false;
			SetProxy(null);
			CoreAPI  = null;
			Instance = null;
		}

		/// <summary>
		/// Identifier of the proxy.
		/// Is for distinguishing between different proxys.
		/// </summary>
		public static string GetId(INoxObject proxy)
			=> proxy?.CallMethod<string>("GetId") ?? string.Empty;

		/// <summary>
		/// Priority of the proxy.
		/// Is used to determine which proxy should be used between multiple proxys.
		/// For example, a proxy (xr) with a higher priority should be used over a proxy (desktop) with a lower priority.
		/// </summary>
		public static int GetPriority(INoxObject proxy)
			=> proxy?.CallMethod<int>("GetPriority") ?? 0;

		/// <summary>
		/// Get transform of a part of the rig.
		/// </summary>
		/// <param name="proxy"></param>
		/// <param name="rig"></param>
		/// <returns></returns>
		public static Transform GetPart(INoxObject proxy, HumanBodyBones rig)
			=> GetPart(proxy, rig.ToPlayerRig().ToIndex());

		public static Transform GetPart(INoxObject proxy, PlayerRig rig)
			=> proxy.CallMethod<Transform>("GetPart", rig.ToIndex());

		/// <summary>
		/// Get transform of a part of the rig, but using the rig identifier.
		/// Note: with this, you can add your own rig parts (like ears, tail, more bones, etc).
		/// </summary>
		/// <param name="proxy"></param>
		/// <param name="rig"></param>
		/// <returns></returns>
		private static Transform GetPart(INoxObject proxy, ushort rig) {
			return proxy.HasMethod("GetPart")
				? proxy.CallMethod<Transform>("GetPart", rig)
				: GetParts(proxy).GetValueOrDefault(rig);
		}

		/// <summary>
		/// Get all parts of the rig.
		/// </summary>
		/// <returns></returns>
		private static Dictionary<ushort, Transform> GetParts(INoxObject proxy)
			=> proxy.CallMethod<Dictionary<ushort, Transform>>("GetParts");

		/// <summary>
		/// Is the default proxy priority.
		/// Is exist for other mod need to make sure that can overriding the default proxy.
		/// </summary>
		/// <returns></returns>
		[NoxPublic(NoxAccess.Method)]
		public int GetDefaultPriority()
			=> DesktopProxy.DefaultPriority;
	}
}