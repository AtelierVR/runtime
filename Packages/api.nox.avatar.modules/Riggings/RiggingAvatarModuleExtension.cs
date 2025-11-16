using System.Linq;
using Nox.Avatars.Rigging;
using UnityEngine;

#if HAS_FINALIK
using RootMotion.FinalIK;
#endif

namespace Nox.CCK.Avatars.Rigging {
	/// <summary>
	/// Extension methods for RiggingAvatarModule to support IK rigging operations
	/// Supporte à la fois RigBuilder (legacy) et FinalIK VR (préféré)
	/// </summary>
	public static class RiggingAvatarModuleExtension {
		public static Transform GetOrAddPart(this RiggingAvatarModule riggingModule, HumanBodyBones bone, Transform parent = null) {
			var part = riggingModule.GetPart(bone);
			if (part) return part;

			var transform = riggingModule.GetBone(bone);
			if (!transform) return null;

			// Use "IK_" prefix to avoid conflicts with Unity's human bone mapping
			var go            = new GameObject($"IK_{bone}");
			var partTransform = go.transform;
			partTransform.SetParent(parent ?? riggingModule.GetAnchor(), false);
			partTransform.position   = transform.position;
			partTransform.rotation   = transform.rotation;
			partTransform.localScale = Vector3.one;

			riggingModule.SetPart(bone, partTransform);

			return partTransform;
		}

		public static bool IsActive(this RiggingAvatarModule riggingModule, HumanBodyBones bone) {
			#if HAS_FINALIK
			// FinalIK VR est toujours actif quand présent
			var vrik = riggingModule.GetVRIK();
			return vrik && vrik.enabled;
			#else
			// Legacy RigBuilder
			var rigBuilder = riggingModule.GetRigBuilder();
			if (!rigBuilder) return false;
			var name = IKRigGenerator.GetRigFromBone(bone);
			return (from layer in rigBuilder.layers
				where layer.rig && layer.rig.name == name
				select layer.active).FirstOrDefault();
			#endif
		}

		public static void SetActive(this RiggingAvatarModule riggingModule, HumanBodyBones bone, bool active) {
			#if HAS_FINALIK
			// FinalIK VR: contrôle les poids individuels
			var vrik = riggingModule.GetVRIK();
			if (!vrik || !vrik.enabled) return;

			float weight = active ? 1f : 0f;

			switch (bone) {
				case HumanBodyBones.Head:
					vrik.solver.spine.headClampWeight = weight;
					break;
				case HumanBodyBones.LeftHand:
					vrik.solver.leftArm.positionWeight = weight;
					break;
				case HumanBodyBones.RightHand:
					vrik.solver.rightArm.positionWeight = weight;
					break;
				case HumanBodyBones.LeftFoot:
					vrik.solver.leftLeg.positionWeight = weight;
					break;
				case HumanBodyBones.RightFoot:
					vrik.solver.rightLeg.positionWeight = weight;
					break;
				case HumanBodyBones.Hips:
					vrik.solver.spine.pelvisPositionWeight = weight;
					break;
			}
			#else
			// Legacy RigBuilder
			var rigBuilder = riggingModule.GetRigBuilder();
			if (!rigBuilder) return;
			
			// Don't modify rigging if the RigBuilder is disabled or not properly initialized
			if (!rigBuilder.enabled) return;
			
			var name = IKRigGenerator.GetRigFromBone(bone);
			foreach (var layer in rigBuilder.layers.Where(layer => layer.rig && layer.rig.name == name))
				layer.active = active;
			
			// Only build if we're not in the middle of animation processing
			// This prevents TransformStreamHandle resolution errors
			if (Application.isPlaying && rigBuilder.isActiveAndEnabled) {
				try {
					rigBuilder.Build();
				}
				catch (System.InvalidOperationException ex) when (ex.Message.Contains("TransformStreamHandle")) {
					// Log warning but don't crash - the build will happen on next frame
					Debug.LogWarning($"RigBuilder.Build() failed due to timing issue: {ex.Message}. Will retry on next frame.");
				}
			}
			#endif
		}
	}
}