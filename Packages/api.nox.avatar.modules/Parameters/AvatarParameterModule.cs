using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.Avatars.Parameters;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.Avatars.Parameters {
	public class AvatarParameterModule : MonoBehaviour, IParameterModule {
		public static bool Check(IAvatarDescriptor descriptor) {
			var modules = descriptor.GetModules<AvatarParameterModule>();

			var module = modules.Length switch {
				1 => modules.FirstOrDefault(),
				0 => descriptor.GetAnchor().AddComponent<AvatarParameterModule>(),
				_ => null
			};

			if (!module) {
				Logger.LogError("Verify that the Avatar prefab has a valid AvatarParameterModule component.");
				return false;
			}

			return true;
		}

		public AvatarParameters parameters;
		public IRuntimeAvatar   Runtime;

		private readonly Dictionary<int, object> _history = new();

		public async UniTask<bool> Setup(IRuntimeAvatar runtimeAvatar) {
			await UniTask.Yield();
			Runtime = runtimeAvatar;
			return true;
		}

		public IParameter[] GetParameters() {
			var animator = Runtime?.GetDescriptor()?.GetAnimator();
			if (!animator)
				return Array.Empty<IParameter>();

			var entries = parameters?.parameters ?? Array.Empty<ParameterEntry>();

			var parametersList = new List<IParameter>();
			var hashSet        = new HashSet<int>();

			// Ajout des paramètres de l'Animator
			foreach (var parameter in animator.parameters) {
				var hash = parameter.nameHash;
				if (hashSet.Contains(hash)) continue;
				var entry = entries.FirstOrDefault(e => e.GetNameHash() == hash);
				parametersList.Add(new AnimatorBaseParameter { Animator = animator, Parameter = parameter, Entry = entry });
				hashSet.Add(hash);
			}

			// Ajout des paramètres des contrôleurs d'animation
			var controllers = GetAllControllers();

			foreach (var controller in controllers) {
				for (var i = 0; i < controller.GetParameterCount(); i++) {
					var controllerParameter = controller.GetParameter(i);
					var hash                = controllerParameter.nameHash;
					if (hashSet.Contains(hash)) continue;
					var entry = entries.FirstOrDefault(e => e.GetNameHash() == hash);
					parametersList.Add(new PlayableBaseParameter { Animator = animator, Controller = controller, Parameter = controllerParameter, Entry = entry });
					hashSet.Add(hash);
				}
			}

			// Ajout des paramètres des modules
			var modules = Runtime?.GetDescriptor()
					?.GetModules()
					.OfType<IParameterGroup>()
					.Where(m => !ReferenceEquals(m, this))
				?? Array.Empty<IParameterGroup>(); // Exclure ce module pour éviter la récursion

			foreach (var module in modules)
			foreach (var moduleParameter in module.GetParameters()) {
				var hash = moduleParameter.GetKey();
				if (hashSet.Contains(hash)) continue;
				parametersList.Add(moduleParameter); // Ajouter directement l'IParameter sans cast
				hashSet.Add(hash);
			}

			return parametersList.ToArray();
		}

		public IParameter GetParameter(string key)
			=> GetParameters().FirstOrDefault(p => p.GetName() == key);

		public IParameter GetParameter(int hash)
			=> GetParameters().FirstOrDefault(p => p.GetKey() == hash);

		private AnimatorControllerPlayable[] GetAllControllers() {
			var animator    = Runtime?.GetDescriptor()?.GetAnimator();
			var controllers = new List<AnimatorControllerPlayable>();
			if (!animator) return controllers.ToArray();
			for (var i = 0; i < animator.playableGraph.GetRootPlayableCount(); i++)
				controllers.AddRange(RecursiveController(animator.playableGraph.GetRootPlayable(i)));
			return controllers.ToArray();
		}

		private static AnimatorControllerPlayable[] RecursiveController(Playable playable) {
			var controllers = new List<AnimatorControllerPlayable>();

			if (!playable.IsValid())
				return controllers.ToArray();

			if (playable.GetPlayableType() == typeof(AnimatorControllerPlayable))
				controllers.Add((AnimatorControllerPlayable)playable);

			for (var i = 0; i < playable.GetInputCount(); i++) {
				var input = playable.GetInput(i);
				controllers.AddRange(RecursiveController(input));
			}

			return controllers.ToArray();
		}

		private static object GetValue(AnimatorControllerParameter parameter)
			=> parameter.type switch {
				AnimatorControllerParameterType.Float => parameter.defaultFloat,
				AnimatorControllerParameterType.Int   => parameter.defaultInt,
				AnimatorControllerParameterType.Bool  => parameter.defaultBool,
				_                                     => null
			};

		public void Update() {
			var animator = Runtime?.GetDescriptor()?.GetAnimator();
			if (!animator) return;

			// Récupération des paramètres de l'Animator
			foreach (var parameter in animator.parameters) {
				var value = GetValue(parameter);
				var hash  = parameter.nameHash;
				if (_history.TryGetValue(hash, out var entry)) {
					if (!Equals(entry, value)) OnParameterChanged(hash, value);
				} else OnParameterAdded(hash, value);
			}

			foreach (var entry in _history)
				if (animator.parameters.All(p => p.nameHash != entry.Key))
					OnParameterRemoved(entry.Key);
		}

		private void OnParameterAdded(int hash, object value) {
			Logger.LogDebug($"Parameter added: {hash} = {value}");
			_history.Add(hash, value);
		}

		private void OnParameterChanged(int hash, object value) {
			_history[hash] = value;
		}

		private void OnParameterRemoved(int hash) {
			Logger.LogDebug($"Parameter removed: {hash}");
			_history.Remove(hash);
		}
	}
}