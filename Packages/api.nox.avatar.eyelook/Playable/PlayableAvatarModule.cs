using Nox.Avatars;
using Nox.CCK.Build;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

namespace Nox.CCK.Avatars.Playable {
	public class PlayableAvatarModule : MonoBehaviour, IAvatarModule {
		public  RuntimeAnimatorController[] controllers;
		private PlayableGraph               _graph;
		private AnimationLayerMixerPlayable _mixer;
		private IAvatarDescriptor           _descriptor;

		private void Start()
			=> OnPlay(_descriptor);

		private void OnValidate()
			=> OnPlay(_descriptor);

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

		public void OnPlay(IAvatarDescriptor descriptor) {
			_descriptor = descriptor;
			if (descriptor == null) {
				Debug.LogError("Avatar descriptor is not set, cannot play avatar module.");
				return;
			}

			var animator = descriptor.GetAnimator();
			if (!animator) {
				Debug.LogError("Animator is not set, cannot play avatar module.");
				return;
			}

			if (!animator.playableGraph.IsValid()) {
				Debug.LogError("Animator's playable graph is not valid, cannot play avatar module.");
				return;
			}

			_graph = animator.playableGraph;
			var output = AnimationPlayableOutput.Create(_graph, "Animation", animator);
			_mixer = AnimationLayerMixerPlayable.Create(_graph, controllers.Length);

			for (var i = 0; i < controllers.Length; i++) {
				var ctrlPlayable = AnimatorControllerPlayable.Create(_graph, controllers[i]);
				_graph.Connect(ctrlPlayable, 0, _mixer, i);
				_mixer.SetInputWeight(i, 1f);
			}

			output.SetSourcePlayable(_mixer);
			if (gameObject.activeInHierarchy)
				_graph.Play();
		}
	}
}