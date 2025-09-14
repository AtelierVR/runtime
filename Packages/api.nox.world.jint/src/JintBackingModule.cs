using System.Collections.Generic;
using System.Linq;
using api.nox.jint;
using Cysharp.Threading.Tasks;
using Nox.CCK.Jint;
using Nox.CCK.Utils;
using Nox.Sessions;
using Nox.Worlds;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.world.jint {
	public class JintBackingModule : MonoBehaviour, ISessionModule {
		#region Internal

		public static bool Check(IWorldDescriptor descriptor) {
			var modules = descriptor.GetModules<JintBackingModule>();

			var module = modules.Length switch {
				1 => modules.FirstOrDefault(),
				0 => descriptor.GetAnchor().AddComponent<JintBackingModule>(),
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

		private ISession          _session;
		public  List<JintBacking> backings = new();

		public void OnSceneLoaded(IWorldDescriptor _0, int _1, GameObject anchor) {
			var scripts = anchor.GetComponentsInChildren<JintScript>(true);
			foreach (var script in scripts) {
				if (backings.Any(b => b.GetInstanceID() == script.GetInstanceID())) continue;
				var backing = script.gameObject.GetOrAddComponent<JintBacking>();
				backings.Add(backing);
			}
		}

		public void OnSceneUnloaded(int index)
			=> backings.RemoveAll(b => !b);

		public void OnLoaded(ISession session)
			=> _session = session;
	}
}