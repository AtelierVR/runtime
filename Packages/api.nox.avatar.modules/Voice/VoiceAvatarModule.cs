using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.Avatars.Voice;
using Nox.CCK.Utils;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;
using Transform = UnityEngine.Transform;

namespace Nox.CCK.Avatars.Voice {
	public class VoiceAvatarModule : MonoBehaviour, IVoiceModule {
		public Vector3   voiceOffset = Vector3.zero;
		public Transform headTransform;

		private AudioSource _audioSource;

		public async UniTask<bool> Setup(IRuntimeAvatar runtimeAvatar) {
			await UniTask.Yield();
			var descriptor = runtimeAvatar.GetDescriptor();

			// Try to get head transform from animator bones
			if (!headTransform) {
				var animator = descriptor.GetAnimator();
				if (animator)
					headTransform = animator.GetBoneTransform(HumanBodyBones.Head);
			}

			// Fallback: search for a transform named "Head" in the hierarchy
			if (headTransform)
				return true;

			var anchor = descriptor.GetAnchor();
			headTransform = anchor.Find("Head")?.transform;
			if (headTransform)
				Logger.LogWarning("Head bone not found in animator, using transform named 'Head' as fallback.", this);

			// Last resort: use the avatar root transform
			if (!headTransform) {
				headTransform = descriptor.GetAnchor().transform;
				Logger.LogWarning($"Head transform not found, using avatar root as fallback.", this);
			}

			var go = new GameObject("Voice Anchor");
			go.transform.SetParent(headTransform, false);
			go.transform.localPosition = voiceOffset;
			go.transform.localRotation = Quaternion.identity;
			go.transform.localScale    = Vector3.one;

			_audioSource = go.AddComponent<AudioSource>();

			return true;
		}

		public AudioSource GetSource()
			=> _audioSource;

		public static bool Check(IAvatarDescriptor descriptor) {
			var modules = descriptor.GetModules<VoiceAvatarModule>();

			var module = modules.Length switch {
				1 => modules.FirstOrDefault(),
				0 => descriptor.GetAnchor().AddComponent<VoiceAvatarModule>(),
				_ => null
			};

			if (module) 
				return true;
			
			Logger.LogError("Verify that the Avatar prefab has a valid CameraAvatarModule component.", descriptor.GetAnchor());
			return false;

		}
	}
}