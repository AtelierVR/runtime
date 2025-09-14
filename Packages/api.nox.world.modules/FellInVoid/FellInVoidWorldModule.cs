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

		public ISession Session;

		public void OnLoaded(ISession session)
			=> Session = session;

		public void Update() {
			if (Session == null) return;
			var local = Session.GetAdapter().GetLocalPlayer();
			var pos   = local.GetPosition();
			if (pos.y < fallThreshold)
				local.Respawn();
		}
	}
}