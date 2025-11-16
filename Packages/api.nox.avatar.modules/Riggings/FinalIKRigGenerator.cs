#if HAS_FINALIK
using Nox.Avatars.Rigging;
using Nox.CCK.Utils;
using UnityEngine;
using RootMotion.FinalIK;
using Transform = UnityEngine.Transform;

namespace Nox.CCK.Avatars.Rigging {
	/// <summary>
	/// Générateur pour les systèmes IK utilisant FinalIK VR (préféré quand disponible)
	/// </summary>
	public static class FinalIKRigGenerator {
		public const string VRIKRoot = "VRIK_Root";

		public static VRIK CreateVRIKRig(RiggingAvatarModule module) {
			var vrik     = module.GetVRIK();
			var animator = module.GetBone(HumanBodyBones.Hips).root.GetComponent<Animator>();

			if (animator == null) {
				Debug.LogError("Animator not found on avatar root!");
				return null;
			}

			// Configuration de VRIK
			vrik.solver.spine.headTarget = module.GetOrAddPart(HumanBodyBones.Head, module.GetAnchor());
			vrik.solver.leftArm.target   = module.GetOrAddPart(HumanBodyBones.LeftHand, module.GetAnchor());
			vrik.solver.rightArm.target  = module.GetOrAddPart(HumanBodyBones.RightHand, module.GetAnchor());

			// Configuration des jambes
			vrik.solver.leftLeg.target  = module.GetOrAddPart(HumanBodyBones.LeftFoot, module.GetAnchor());
			vrik.solver.rightLeg.target = module.GetOrAddPart(HumanBodyBones.RightFoot, module.GetAnchor());

			// Configuration des orteils si disponibles
			var leftToes  = module.GetBone(HumanBodyBones.LeftToes);
			var rightToes = module.GetBone(HumanBodyBones.RightToes);

			if (leftToes != null) {
				vrik.solver.leftLeg.bendGoal = module.GetOrAddPart(HumanBodyBones.LeftToes, module.GetAnchor());
			}

			if (rightToes != null) {
				vrik.solver.rightLeg.bendGoal = module.GetOrAddPart(HumanBodyBones.RightToes, module.GetAnchor());
			}

			// Configuration des coudes (hints)
			vrik.solver.leftArm.bendGoal  = module.GetOrAddPart(HumanBodyBones.LeftLowerArm, module.GetAnchor());
			vrik.solver.rightArm.bendGoal = module.GetOrAddPart(HumanBodyBones.RightLowerArm, module.GetAnchor());

			// Configuration du pelvis
			vrik.solver.spine.pelvisTarget = module.GetOrAddPart(HumanBodyBones.Hips, module.GetAnchor());

		// Poids par défaut optimisés pour VR
		vrik.solver.spine.headClampWeight      = 1f;
		vrik.solver.leftArm.positionWeight     = 1f;
		vrik.solver.rightArm.positionWeight    = 1f;
		vrik.solver.leftLeg.positionWeight     = 1f;
		vrik.solver.rightLeg.positionWeight    = 1f;
		vrik.solver.spine.pelvisPositionWeight = 0f; // Désactivé pour que la tête tire le corps
		
		// Activer les rotations pour un meilleur rendu VR
		vrik.solver.leftArm.rotationWeight  = 1f;
		vrik.solver.rightArm.rotationWeight = 1f;

		// Configuration pour que la tête tire tout le corps sans déplacer la racine
		vrik.solver.locomotion.mode = IKSolverVR.Locomotion.Mode.Animated;
		
		// Configuration de la locomotion pour stabilité
		vrik.solver.locomotion.footDistance = 0.3f;
		vrik.solver.locomotion.stepThreshold = 0.4f;
		vrik.solver.locomotion.angleThreshold = 60f;
		vrik.solver.locomotion.maxVelocity   = 0.4f;
		vrik.solver.locomotion.velocityFactor = 0.4f;
		vrik.solver.locomotion.rootSpeed     = 40f;
		vrik.solver.locomotion.stepSpeed     = 3f;
		
		// Configuration du spine pour suivre la tête sans déplacer le root
		vrik.solver.spine.maintainPelvisPosition = 0.5f; // Équilibre entre suivre la tête et rester stable
		vrik.solver.spine.positionWeight         = 1f;
		vrik.solver.spine.rotationWeight         = 1f;
		vrik.solver.spine.pelvisRotationWeight   = 0.2f; // Rotation limitée du pelvis
		vrik.solver.spine.chestGoalWeight        = 0f;
		vrik.solver.plantFeet                    = true; // Garder les pieds au sol pour stabilité
		vrik.solver.spine.neckStiffness          = 0f;
		vrik.solver.spine.maxRootAngle           = 180f;
		
		UpdateParts(module);

			return vrik;
		}

		private static void UpdateParts(RiggingAvatarModule module) {
			// Mise à jour des parts si nécessaire
		}
	}
}
#endif