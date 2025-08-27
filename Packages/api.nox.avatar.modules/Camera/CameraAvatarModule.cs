using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.Avatars.Camera;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.Avatars.Camera {
	public class CameraAvatarModule : MonoBehaviour, ICameraModule {
		public Vector3   cameraOffset = Vector3.zero;
		public Transform headTransform;


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

			if (cameraOffset == Vector3.zero) {
				Logger.LogWarning("CameraOffset is not set, defaulting to head position.");
				var leftEye = descriptor
					.GetAnimator()
					?.GetBoneTransform(HumanBodyBones.LeftEye);

				var rightEye = descriptor
					.GetAnimator()
					?.GetBoneTransform(HumanBodyBones.RightEye);

				if (leftEye && rightEye) {
					cameraOffset = (leftEye.position + rightEye.position) / 2 - headTransform.position;
				} else if (leftEye) {
					cameraOffset = leftEye.position - headTransform.position;
				} else if (rightEye) {
					cameraOffset = rightEye.position - headTransform.position;
				}

				cameraOffset.x = 0f;
				cameraOffset.z = cameraOffset.y * 3f;
			}

			return true;
		}

		public static bool Check(IAvatarDescriptor descriptor) {
			var modules = descriptor.GetModules<CameraAvatarModule>();

			var module = modules.Length switch {
				1 => modules.FirstOrDefault(),
				0 => descriptor.GetRoot().AddComponent<CameraAvatarModule>(),
				_ => null
			};

			if (!module) {
				Logger.LogError("Verify that the Avatar prefab has a valid CameraAvatarModule component.");
				return false;
			}

			return true;
		}

		public Vector3 GetOffset()
			=> cameraOffset;

		public Transform GetAnchor()
			=> headTransform;
	}
}