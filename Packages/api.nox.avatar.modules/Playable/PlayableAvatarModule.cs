using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.Avatars.StateMachines;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.Avatars.Playable {
	public class PlayableAvatarModule : MonoBehaviour, IAvatarModule {
		public static Func<RuntimeAnimatorController> GetAssetController;

		public  RuntimeAnimatorController[]  controllers;
		private PlayableGraph                _graph;
		private AnimationLayerMixerPlayable  _mixer;
		public  AnimatorControllerPlayable[] ControllerPlayables { get; private set; }
		public  IAvatarDescriptor            Descriptor;

		private void Start() {
			if (!_graph.IsValid()) return;
			_graph.Play();
		}

		private void OnEnable() {
			if (!_graph.IsValid()) return;
			_graph.Play();
		}

		private void OnDisable() {
			if (!_graph.IsValid()) return;
			_graph.Stop();
		}

		private void OnDestroy() {
			if (!_graph.IsValid()) return;
			_graph.Destroy();
		}

		public async UniTask<bool> Setup(IRuntimeAvatar runtimeAvatar) {
			Descriptor = runtimeAvatar.GetDescriptor();

			if (Descriptor == null) {
				Logger.LogError("Avatar descriptor is not set, cannot play avatar module.");
				return false;
			}

			var animator = Descriptor.GetAnimator();

			if (!animator) {
				Logger.LogWarning("Animator is not set, PlayableAvatarModule will be disabled for this avatar.");
				return false;
			}

			animator.runtimeAnimatorController ??= GetAssetController();

			if (!animator.playableGraph.IsValid()) {
				Descriptor.GetAnchor().SetActive(true);
				var startTime = Time.realtimeSinceStartup;
				await UniTask.WaitUntil(() => animator.playableGraph.IsValid() || Time.realtimeSinceStartup - startTime > 15f);
				Descriptor.GetAnchor().SetActive(false);
				if (Time.realtimeSinceStartup - startTime > 15) {
					Logger.LogError("Timed out waiting for Animator's playable graph to become valid, cannot play avatar module.");
					return false;
				}
			}


			if (!animator.playableGraph.IsValid()) {
				Logger.LogError("Animator's playable graph is not valid, cannot play avatar module.");
				return false;
			}

			controllers ??= Array.Empty<RuntimeAnimatorController>();
			if (controllers.Length == 0) {
				Logger.LogWarning("No controllers set for PlayableAvatarModule, skipping setup.");
				return false;
			}

			_graph = animator.playableGraph;
			var output = AnimationPlayableOutput.Create(_graph, "Animation", animator);
			_mixer = AnimationLayerMixerPlayable.Create(_graph, controllers.Length);

			ControllerPlayables = new AnimatorControllerPlayable[controllers.Length];
			for (var i = 0; i < controllers.Length; i++) {
				var ctrlPlayable = AnimatorControllerPlayable.Create(_graph, controllers[i]);
				_graph.Connect(ctrlPlayable, 0, _mixer, i);
				_mixer.SetInputWeight(i, 1f);
				ControllerPlayables[i] = ctrlPlayable;
			}

			output.SetSourcePlayable(_mixer);

			foreach (var behavior in animator.GetBehaviours<StateMachineBehaviour>()) {
				if (behavior is not IStateMachine state) continue;
				state.Setup(runtimeAvatar);
			}

			return true;
		}

		public static bool Check(IAvatarDescriptor descriptor)
			=> true;
	}
}