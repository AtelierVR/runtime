using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.Players;
using Nox.Sessions;
using Nox.Worlds;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.Worlds.FellInVoid {
	public class FellInVoidWorldModule : MonoBehaviour, ISessionModule {
		#region Internal

		public static bool Check(IWorldDescriptor descriptor) {
			var modules = descriptor.GetModules<FellInVoidWorldModule>();

			var module = modules.Length switch {
				1 => modules.FirstOrDefault(),
				0 => descriptor.GetAnchor().AddComponent<FellInVoidWorldModule>(),
				_ => null
			};

			if (!module) {
				Logger.LogError("Verify that the World prefab has a valid FellInVoidWorldModule component.");
				return false;
			}

			return true;
		}

		public UniTask<bool> Setup(IRuntimeWorld runtime)
			=> UniTask.FromResult(true);

		#endregion

		public float fallThreshold = -100f;

		private ISession _session;

		public void OnSession(ISession session)
			=> _session = session;

		public void OnPlayerJoined(IPlayer player) {
			Logger.Log("OnPlayerJoined: " + player);
		}

		public void OnPlayerLeft(IPlayer player) {
			Logger.Log("OnPlayerLeft: " + player);
		}

		public void Update() {
			var local = _session?.GetAdapter().GetLocalPlayer();
			local.Respawn();
		}
	}
}