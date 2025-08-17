using System;
using System.Collections.Generic;
using System.Linq;
using Nox.Avatars;
using Nox.Avatars.Parameters;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.Avatars.Parameters {
	public class AvatarParameterModule : MonoBehaviour, IParameterModule {
		public AvatarParameters  parameters;
		public IAvatarDescriptor Descriptor;

		private readonly Dictionary<int, object> _history = new();

		public void OnPlay(IAvatarDescriptor descriptor)
			=> Descriptor = descriptor;


		public IParameter[] GetParameters() {
			var animator = Descriptor.GetAnimator();
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
			var modules = Descriptor.GetModules()
				.OfType<IParameterGroup>()
				.Where(m => !ReferenceEquals(m, this)); // Exclure ce module pour éviter la récursion

			foreach (var module in modules)
			foreach (var moduleParameter in module.GetParameters()) {
				var hash = moduleParameter.GetHash();
				if (hashSet.Contains(hash)) continue;
				parametersList.Add(moduleParameter); // Ajouter directement l'IParameter sans cast
				hashSet.Add(hash);
			}

			return parametersList.ToArray();
		}

		public int GetParameterCount()
			=> GetParameters().Length;

		public IParameter GetParameter(int index) {
			var ps = GetParameters();
			if (index < 0 || index >= ps.Length)
				throw new IndexOutOfRangeException($"Index {index} is out of range for parameters array of length {ps.Length}");
			return ps[index];
		}

		private AnimatorControllerPlayable[] GetAllControllers() {
			var animator    = Descriptor.GetAnimator();
			var controllers = new List<AnimatorControllerPlayable>();
			if (!animator) return controllers.ToArray();
			for (var i = 0; i < animator.playableGraph.GetRootPlayableCount(); i++)
				controllers.AddRange(RecursiveController(animator.playableGraph.GetRootPlayable(i)));
			return controllers.ToArray();
		}

		private static AnimatorControllerPlayable[] RecursiveController(UnityEngine.Playables.Playable playable) {
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
			var animator = Descriptor.GetAnimator();
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

	public class PlayableBaseParameter : BaseParameter {
		internal AnimatorControllerPlayable Controller;
		internal Animator                   Animator;

		protected override void SetFloat(float value)
			=> Animator.SetFloat(GetHash(), value);

		protected override void SetInteger(int value)
			=> Animator.SetInteger(GetHash(), value);

		protected override void SetBool(bool value)
			=> Animator.SetBool(GetHash(), value);

		protected override bool GetBool()
			=> Controller.GetBool(GetHash());

		protected override float GetFloat()
			=> Controller.GetFloat(GetHash());

		protected override int GetInteger()
			=> Controller.GetInteger(GetHash());
	}

	public class AnimatorBaseParameter : BaseParameter {
		internal Animator Animator;

		protected override void SetFloat(float value)
			=> Animator.SetFloat(GetHash(), value);

		protected override void SetInteger(int value)
			=> Animator.SetInteger(GetHash(), value);

		protected override void SetBool(bool value)
			=> Animator.SetBool(GetHash(), value);

		protected override float GetFloat()
			=> Animator.GetFloat(GetHash());

		protected override int GetInteger()
			=> Animator.GetInteger(GetHash());

		protected override bool GetBool()
			=> Animator.GetBool(GetHash());
	}


	public abstract class BaseParameter : IParameter {
		internal AnimatorControllerParameter Parameter;
		internal ParameterEntry              Entry;

		public string GetName()
			=> Parameter.name;

		public int GetHash()
			=> Parameter.nameHash;

		public bool IsSyncable()
			=> Entry?.synced ?? false;

		public bool IsSavable()
			=> Entry?.savable ?? false;

		public bool IsReadOnly()
			=> false;

		public ParameterType GetValueType()
			=> Parameter.type switch {
				AnimatorControllerParameterType.Float => ParameterType.Float,
				AnimatorControllerParameterType.Int   => ParameterType.Int,
				AnimatorControllerParameterType.Bool  => ParameterType.Bool,
				_                                     => ParameterType.Float
			};

		public object Get()
			=> Parameter.type switch {
				AnimatorControllerParameterType.Float => GetFloat(),
				AnimatorControllerParameterType.Int   => GetInteger(),
				AnimatorControllerParameterType.Bool  => GetBool(),
				_                                     => throw new ArgumentOutOfRangeException()
			};

		public void Set(object value) {
			switch (GetValueType()) {
				case ParameterType.Float:
					SetFloat((float)value);
					break;
				case ParameterType.Int:
					SetInteger((int)value);
					break;
				case ParameterType.Bool:
					SetBool((bool)value);
					break;
				default:
					throw new ArgumentOutOfRangeException($"Unsupported parameter type: {Parameter.type}");
			}
		}

		protected abstract void  SetFloat(float value);
		protected abstract void  SetInteger(int value);
		protected abstract void  SetBool(bool   value);
		protected abstract float GetFloat();
		protected abstract int   GetInteger();
		protected abstract bool  GetBool();
	}
}