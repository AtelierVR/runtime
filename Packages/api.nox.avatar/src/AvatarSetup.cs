using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.CCK.Build;
using Nox.CCK.Utils;

namespace api.nox.avatar {
	public class AvatarSetup {
		public static async UniTask<bool> Prepare(IRuntimeAvatar avatar, Action<float> progress = null, CancellationToken token = default) {
			if (avatar == null) {
				Logger.LogError("Avatar descriptor is null.");
				return false;
			}

			progress?.Invoke(0.0f);

			// Vérifier l'annulation dès le début
			if (token.IsCancellationRequested)
				return false;

			var descriptor = avatar.GetDescriptor();
			var gameObject = descriptor.GetRoot();

			if (!gameObject) {
				Logger.LogError("Avatar descriptor root GameObject is null.");
				return false;
			}

			descriptor.FindModules();

			var valid = true;
			Main.Instance.CoreAPI.EventAPI.Emit("avatar_check_request", descriptor, new Action<object[]>(OnCheckRequest));

			if (!valid) {
				Logger.LogError("A mod asked to cancel the avatar preparation.");
				return false;
			}

			descriptor.FindModules();

			if (token.IsCancellationRequested)
				return false;

			progress?.Invoke(0.1f);

			var compilable = gameObject
				.GetComponentsInChildren<ICompilable>(true)
				.OrderBy(c => c.CompileOrder)
				.ToArray();

			// Compilation des composants avec progression
			for (var i = 0; i < compilable.Length; i++) {
				if (token.IsCancellationRequested)
					return false;

				var c = compilable[i];
				if (c == null) {
					Logger.LogWarning($"Compilable component at index {i} is null, skipping.");
					continue;
				}

				Logger.LogDebug($"Compiling {c.GetType().Name} ({i + 1}/{compilable.Length})...");
				c.Compile();
				await c.CompileAsync();

				// Rapporter la progression (10% à 70% pour la compilation)
				var compileProgress = 0.2f + 0.5f * (i + 1) / compilable.Length;
				progress?.Invoke(compileProgress);
			}

			if (token.IsCancellationRequested)
				return false;
			progress?.Invoke(0.8f);

			var modules     = descriptor.GetModules();
			var moduleArray = modules.ToArray();

			// Initialisation des modules avec progression
			for (var i = 0; i < moduleArray.Length; i++) {
				if (token.IsCancellationRequested)
					return false;

				if (!await moduleArray[i].Setup(avatar)) {
					Logger.LogError($"Module {moduleArray[i].GetType().Name} failed to initialize.");
					return false;
				}

				// Rapporter la progression (80% à 100% pour les modules)
				var moduleProgress = 0.8f + 0.2f * (i + 1) / moduleArray.Length;
				progress?.Invoke(moduleProgress);
			}

			progress?.Invoke(1.0f);
			return true;

			void OnCheckRequest(object[] args) {
				if (args.Length > 0 && args[0] is false)
					valid = false;
			}
		}
	}
}