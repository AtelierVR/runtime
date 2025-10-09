using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.Avatars.Voice;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.Avatars.Voice {
	public class VoiceAvatarModule : MonoBehaviour, IVoiceModule {
		public Vector3   voiceOffset = Vector3.zero;
		public Transform headTransform;

		private AudioSource _audioSource;


		public async UniTask<bool> Setup(IRuntimeAvatar runtimeAvatar) {
			await UniTask.Yield();
			var descriptor = runtimeAvatar.GetDescriptor();
			headTransform ??= descriptor
				.GetAnimator()
				?.GetBoneTransform(HumanBodyBones.Head);

			if (!headTransform) {
				Logger.LogError("Head transform is not set, cannot play CameraAvatarModule.");
				return false;
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

			if (!module) {
				Logger.LogError("Verify that the Avatar prefab has a valid CameraAvatarModule component.");
				return false;
			}

			return true;
		}
	}
}