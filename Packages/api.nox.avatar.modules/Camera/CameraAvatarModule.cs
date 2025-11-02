using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.Avatars.Camera;
using Nox.CCK.Utils;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;
using Transform = UnityEngine.Transform;

namespace Nox.CCK.Avatars.Camera {
	public class CameraAvatarModule : MonoBehaviour, ICameraModule {
		public Vector3   cameraOffset = Vector3.zero;
		public Transform headTransform;


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
			if (!headTransform) {
				var anchor = descriptor.GetAnchor();
				headTransform = anchor.Find("Head")?.transform;
				if (headTransform) 
					Logger.LogWarning("Head bone not found in animator, using transform named 'Head' as fallback.", this);
			}

			// Last resort: use the avatar root transform
			if (!headTransform) 
				headTransform = descriptor.GetAnchor().transform;

			if (cameraOffset != Vector3.zero) 
				return true;
			
			Logger.LogWarning("CameraOffset is not set, defaulting to head position.", this);
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
			} else if (rightEye) 
				cameraOffset = rightEye.position - headTransform.position;

			cameraOffset.x = 0f;
			cameraOffset.z = cameraOffset.y * 3f;

			return true;
		}

		public static bool Check(IAvatarDescriptor descriptor) {
			var modules = descriptor.GetModules<CameraAvatarModule>();

			var module = modules.Length switch {
				1 => modules.FirstOrDefault(),
				0 => descriptor.GetAnchor().AddComponent<CameraAvatarModule>(),
				_ => null
			};

			if (module) 
				return true;
			
			Logger.LogError("Verify that the Avatar prefab has a valid CameraAvatarModule component.", descriptor.GetAnchor());
			return false;

		}

		public Vector3 GetOffset()
			=> cameraOffset;

		public Transform GetAnchor()
			=> headTransform;
	}
}