using System;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.Avatars.Rigging {
	/// <summary>
	/// Générateur statique pour les systèmes IK de rigging avatar
	/// </summary>
	public static class IKRigGenerator {
		
		/// <summary>
		/// Crée ou récupère un container de rig
		/// </summary>
		/// <param name="parent">Transform parent</param>
		/// <param name="containerName">Nom du container (par défaut "RigContainer")</param>
		/// <returns>Transform du container de rig</returns>
		public static Transform CreateOrGetRigContainer(Transform parent, string containerName = "RigContainer") {
			var existing = parent.Find(containerName);
			if (existing) return existing;
			
			var rigContainer = new GameObject(containerName);
			rigContainer.transform.SetParent(parent);
			rigContainer.transform.localPosition = Vector3.zero;
			rigContainer.transform.localRotation = Quaternion.identity;
			return rigContainer.transform;
		}

		/// <summary>
		/// Crée un nouveau rig avec le composant Rig
		/// </summary>
		/// <param name="parent">Transform parent</param>
		/// <param name="rigName">Nom du rig</param>
		/// <param name="weight">Poids initial du rig (par défaut 1.0f)</param>
		/// <returns>Transform du rig créé</returns>
		public static Transform CreateRig(Transform parent, string rigName, float weight = 1f) {
			var rigGo = new GameObject(rigName);
			rigGo.transform.SetParent(parent);
			rigGo.transform.localPosition = Vector3.zero;
			rigGo.transform.localRotation = Quaternion.identity;
			
			var rig = rigGo.AddComponent<Rig>();
			rig.weight = weight;
			return rigGo.transform;
		}
		
		/// <summary>
		/// Génère un système IK pour les bras avec support VR tracking
		/// </summary>
		/// <param name="animator">Animator de l'avatar</param>
		/// <param name="rigContainer">Container pour les contraintes</param>
		/// <param name="isLeft">True pour le bras gauche, false pour le droit</param>
		/// <param name="handTarget">Target de la main (sera créé si null)</param>
		/// <param name="elbowTarget">Target du coude (sera créé si null)</param>
		/// <param name="shoulderTarget">Target de l'épaule (sera créé si null)</param>
		public static void GenerateArmIK(Animator animator, Transform rigContainer, bool isLeft,
			ref Transform handTarget, ref Transform elbowTarget, ref Transform shoulderTarget) {
			
			var prefix = isLeft ? "Left" : "Right";
			var shoulderBone = isLeft ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm;
			var forearmBone = isLeft ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm;
			var handBone = isLeft ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand;

			var shoulder = animator.GetBoneTransform(shoulderBone);
			var forearm = animator.GetBoneTransform(forearmBone);
			var hand = animator.GetBoneTransform(handBone);

			if (!shoulder || !forearm || !hand) {
				Logger.LogWarning($"Missing bones for {prefix} arm IK generation");
				return;
			}

			// Create or assign target for the hand
			if (!handTarget) {
				handTarget = CreateTarget(rigContainer, $"{prefix}HandTarget", hand.position, hand.rotation);
			}

			// Create or assign elbow target for VR tracking
			if (!elbowTarget) {
				elbowTarget = CreateTarget(rigContainer, $"{prefix}ElbowTarget", forearm.position, forearm.rotation);
			}

			// Create or assign shoulder target for VR tracking
			if (!shoulderTarget) {
				shoulderTarget = CreateTarget(rigContainer, $"{prefix}ShoulderTarget", shoulder.position, shoulder.rotation);
			}

			// Create TwoBoneIK constraint using elbowTarget as hint
			var constraintGo = new GameObject($"{prefix}ArmIK");
			constraintGo.transform.SetParent(rigContainer);

			var ikConstraint = constraintGo.AddComponent<TwoBoneIKConstraint>();
			ikConstraint.data.root = shoulder;
			ikConstraint.data.mid = forearm;
			ikConstraint.data.tip = hand;
			ikConstraint.data.target = handTarget;
			ikConstraint.data.hint = elbowTarget; // Use elbowTarget directly as hint
			ikConstraint.data.targetPositionWeight = 1f;
			ikConstraint.data.targetRotationWeight = 1f;
			ikConstraint.data.hintWeight = 1f; // Full weight for direct control
			ikConstraint.weight = 1f;

			// Add constraint for shoulder tracking
			GenerateShoulderConstraint(animator, rigContainer, shoulderTarget, isLeft);
			
			Logger.Log($"Generated {prefix} arm IK with VR tracking support");
		}

		/// <summary>
		/// Génère un système IK pour les jambes avec support VR tracking
		/// </summary>
		/// <param name="animator">Animator de l'avatar</param>
		/// <param name="rigContainer">Container pour les contraintes</param>
		/// <param name="isLeft">True pour la jambe gauche, false pour la droite</param>
		/// <param name="footTarget">Target du pied (sera créé si null)</param>
		/// <param name="kneeTarget">Target du genou (sera créé si null)</param>
		public static void GenerateLegIK(Animator animator, Transform rigContainer, bool isLeft,
			ref Transform footTarget, ref Transform kneeTarget) {
			
			var prefix = isLeft ? "Left" : "Right";
			var upperLegBone = isLeft ? HumanBodyBones.LeftUpperLeg : HumanBodyBones.RightUpperLeg;
			var lowerLegBone = isLeft ? HumanBodyBones.LeftLowerLeg : HumanBodyBones.RightLowerLeg;
			var footBone = isLeft ? HumanBodyBones.LeftFoot : HumanBodyBones.RightFoot;

			var upperLeg = animator.GetBoneTransform(upperLegBone);
			var lowerLeg = animator.GetBoneTransform(lowerLegBone);
			var foot = animator.GetBoneTransform(footBone);

			if (!upperLeg || !lowerLeg || !foot) {
				Logger.LogWarning($"Missing bones for {prefix} leg IK generation");
				return;
			}

			// Create or assign target for the foot
			if (!footTarget) {
				footTarget = CreateTarget(rigContainer, $"{prefix}FootTarget", foot.position, foot.rotation);
			}

			// Create or assign knee target for VR tracking
			if (!kneeTarget) {
				kneeTarget = CreateTarget(rigContainer, $"{prefix}KneeTarget", lowerLeg.position, lowerLeg.rotation);
			}

			// Create hint target for knee direction (different from knee target for VR)
			var hintTargetGo = new GameObject($"{prefix}KneeHint");
			hintTargetGo.transform.SetParent(rigContainer);
			hintTargetGo.transform.position = lowerLeg.position + Vector3.forward * 0.3f;

			// Create TwoBoneIK constraint
			var constraintGo = new GameObject($"{prefix}LegIK");
			constraintGo.transform.SetParent(rigContainer);

			var ikConstraint = constraintGo.AddComponent<TwoBoneIKConstraint>();
			ikConstraint.data.root = upperLeg;
			ikConstraint.data.mid = lowerLeg;
			ikConstraint.data.tip = foot;
			ikConstraint.data.target = footTarget;
			ikConstraint.data.hint = hintTargetGo.transform;
			ikConstraint.data.targetPositionWeight = 1f;
			ikConstraint.data.targetRotationWeight = 1f;
			ikConstraint.data.hintWeight = 0.5f;
			ikConstraint.weight = 1f;

			// Add constraint for knee tracking
			GenerateKneeConstraint(animator, rigContainer, kneeTarget, isLeft);
			
			Logger.Log($"Generated {prefix} leg IK with VR tracking support");
		}

		/// <summary>
		/// Génère un système IK/contrainte pour la tête et le cou
		/// </summary>
		/// <param name="animator">Animator de l'avatar</param>
		/// <param name="rigContainer">Container pour les contraintes</param>
		/// <param name="headTarget">Target de la tête (sera créé si null)</param>
		/// <param name="neckTarget">Target du cou (sera créé si null)</param>
		/// <param name="chestTarget">Target de la poitrine (sera créé si null)</param>
		/// <param name="spineTarget">Target de la colonne (sera créé si null)</param>
		/// <param name="useHeadTwoBoneIK">Utiliser TwoBoneIK au lieu de MultiAim</param>
		public static void GenerateHeadRig(Animator animator, Transform rigContainer, ref Transform headTarget,
			ref Transform neckTarget, ref Transform chestTarget, ref Transform spineTarget, bool useHeadTwoBoneIK = true) {
			
			var headTransform = animator.GetBoneTransform(HumanBodyBones.Head);
			var neckTransform = animator.GetBoneTransform(HumanBodyBones.Neck);
			var chestTransform = animator.GetBoneTransform(HumanBodyBones.Chest);
			var spineTransform = animator.GetBoneTransform(HumanBodyBones.Spine);

			if (!headTransform) {
				Logger.LogWarning("Missing head bone for head rig generation");
				return;
			}

			// Create targets if not assigned
			if (!headTarget) {
				headTarget = CreateTarget(rigContainer, "HeadTarget", headTransform.position);
			}

			if (!neckTarget && neckTransform) {
				neckTarget = CreateTarget(rigContainer, "NeckTarget", neckTransform.position, neckTransform.rotation);
			}

			if (!chestTarget && chestTransform) {
				chestTarget = CreateTarget(rigContainer, "ChestTarget", chestTransform.position, chestTransform.rotation);
			}

			if (!spineTarget && spineTransform) {
				spineTarget = CreateTarget(rigContainer, "SpineTarget", spineTransform.position, spineTransform.rotation);
			}

			// Create constraint GameObject
			var constraintGo = new GameObject("HeadConstraint");
			constraintGo.transform.SetParent(rigContainer);

			// Toujours créer les deux contraintes - TwoBoneIK et LookAt
			TwoBoneIKConstraint ikConstraint = null;
			MultiAimConstraint aimConstraint = null;

			// Créer TwoBoneIK si on a neck et chest
			if (neckTransform != null && chestTransform != null) {
				// Create hint target for neck direction
				var hintTargetGo = new GameObject("NeckHint");
				hintTargetGo.transform.SetParent(rigContainer);
				hintTargetGo.transform.position = neckTransform.position + neckTransform.up * 0.2f;

				// Add TwoBoneIK constraint for Chest/Neck/Head chain
				ikConstraint = constraintGo.AddComponent<TwoBoneIKConstraint>();
				ikConstraint.data.root = chestTransform;
				ikConstraint.data.mid = neckTransform;
				ikConstraint.data.tip = headTransform;
				ikConstraint.data.target = headTarget;
				ikConstraint.data.hint = hintTargetGo.transform;
				ikConstraint.data.targetPositionWeight = 1f;
				ikConstraint.data.targetRotationWeight = 1f;
				ikConstraint.data.hintWeight = 0.3f; // Lower weight for more natural movement
				ikConstraint.weight = useHeadTwoBoneIK ? 1f : 0f;
			}

			// Toujours créer MultiAim constraint pour head look
			aimConstraint = constraintGo.AddComponent<MultiAimConstraint>();
			aimConstraint.data.constrainedObject = headTransform;
			aimConstraint.data.sourceObjects.Add(new WeightedTransform(headTarget, 1f));
			aimConstraint.data.aimAxis = MultiAimConstraintData.Axis.Z;
			aimConstraint.data.upAxis = MultiAimConstraintData.Axis.Y;
			aimConstraint.data.worldUpType = MultiAimConstraintData.WorldUpType.Vector;
			aimConstraint.data.maintainOffset = false;
			aimConstraint.weight = useHeadTwoBoneIK ? 0f : 1f;

			// Generate constraints for additional body parts
			if (neckTarget && neckTransform) {
				GenerateNeckConstraint(animator, rigContainer, neckTarget);
			}

			if (chestTarget && chestTransform) {
				GenerateChestConstraint(animator, rigContainer, chestTarget);
			}

			if (spineTarget && spineTransform) {
				GenerateSpineConstraint(animator, rigContainer, spineTarget);
			}
			
			Logger.Log("Generated head rig with VR tracking support");
		}

		/// <summary>
		/// Génère un système de contrainte pour les hanches
		/// </summary>
		/// <param name="animator">Animator de l'avatar</param>
		/// <param name="rigContainer">Container pour les contraintes</param>
		/// <param name="hipTarget">Target des hanches (sera créé si null)</param>
		public static void GenerateHipIK(Animator animator, Transform rigContainer, ref Transform hipTarget) {
			var hipTransform = animator.GetBoneTransform(HumanBodyBones.Hips);
			if (!hipTransform) {
				Logger.LogWarning("Missing hip bone for hip IK generation");
				return;
			}

			// Create target if not assigned
			if (!hipTarget) {
				hipTarget = CreateTarget(rigContainer, "HipTarget", hipTransform.position, hipTransform.rotation);
			}

			// Create constraint GameObject
			var constraintGo = new GameObject("HipConstraint");
			constraintGo.transform.SetParent(rigContainer);

			// Add MultiPosition constraint for hip movement
			var positionConstraint = constraintGo.AddComponent<MultiPositionConstraint>();
			positionConstraint.data.constrainedObject = hipTransform;
			positionConstraint.data.sourceObjects.Add(new WeightedTransform(hipTarget, 1f));
			positionConstraint.data.constrainedXAxis = true;
			positionConstraint.data.constrainedYAxis = true;
			positionConstraint.data.constrainedZAxis = true;
			positionConstraint.weight = 1f;

			// Add MultiRotation constraint for hip rotation
			var rotationConstraint = constraintGo.AddComponent<MultiRotationConstraint>();
			rotationConstraint.data.constrainedObject = hipTransform;
			rotationConstraint.data.sourceObjects.Add(new WeightedTransform(hipTarget, 1f));
			rotationConstraint.data.constrainedXAxis = true;
			rotationConstraint.data.constrainedYAxis = true;
			rotationConstraint.data.constrainedZAxis = true;
			rotationConstraint.weight = 1f;
			
			Logger.Log("Generated hip IK with VR tracking support");
		}

		/// <summary>
		/// Génère une contrainte pour l'épaule
		/// </summary>
		private static void GenerateShoulderConstraint(Animator animator, Transform rigContainer, Transform shoulderTarget, bool isLeft) {
			var prefix = isLeft ? "Left" : "Right";
			var shoulderBone = isLeft ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm;
			var shoulder = animator.GetBoneTransform(shoulderBone);

			if (!shoulder || !shoulderTarget) return;

			// Create constraint GameObject
			var constraintGo = new GameObject($"{prefix}ShoulderConstraint");
			constraintGo.transform.SetParent(rigContainer);

			// Add MultiPosition constraint for shoulder movement
			var positionConstraint = constraintGo.AddComponent<MultiPositionConstraint>();
			positionConstraint.data.constrainedObject = shoulder;
			positionConstraint.data.sourceObjects.Add(new WeightedTransform(shoulderTarget, 1f));
			positionConstraint.data.constrainedXAxis = true;
			positionConstraint.data.constrainedYAxis = true;
			positionConstraint.data.constrainedZAxis = true;
			positionConstraint.weight = 0.5f; // Reduced weight for natural movement

			// Add MultiRotation constraint for shoulder rotation
			var rotationConstraint = constraintGo.AddComponent<MultiRotationConstraint>();
			rotationConstraint.data.constrainedObject = shoulder;
			rotationConstraint.data.sourceObjects.Add(new WeightedTransform(shoulderTarget, 1f));
			rotationConstraint.data.constrainedXAxis = true;
			rotationConstraint.data.constrainedYAxis = true;
			rotationConstraint.data.constrainedZAxis = true;
			rotationConstraint.weight = 0.3f; // Low weight to avoid conflicts with arm IK
		}

		/// <summary>
		/// Génère une contrainte pour le genou
		/// </summary>
		private static void GenerateKneeConstraint(Animator animator, Transform rigContainer, Transform kneeTarget, bool isLeft) {
			var prefix = isLeft ? "Left" : "Right";
			var lowerLegBone = isLeft ? HumanBodyBones.LeftLowerLeg : HumanBodyBones.RightLowerLeg;
			var lowerLeg = animator.GetBoneTransform(lowerLegBone);

			if (!lowerLeg || !kneeTarget) return;

			// Create constraint GameObject
			var constraintGo = new GameObject($"{prefix}KneeConstraint");
			constraintGo.transform.SetParent(rigContainer);

			// Add MultiPosition constraint for knee position
			var positionConstraint = constraintGo.AddComponent<MultiPositionConstraint>();
			positionConstraint.data.constrainedObject = lowerLeg;
			positionConstraint.data.sourceObjects.Add(new WeightedTransform(kneeTarget, 1f));
			positionConstraint.data.constrainedXAxis = true;
			positionConstraint.data.constrainedYAxis = true;
			positionConstraint.data.constrainedZAxis = true;
			positionConstraint.weight = 0.3f; // Low weight to guide without overriding IK

			// Add MultiRotation constraint for knee rotation
			var rotationConstraint = constraintGo.AddComponent<MultiRotationConstraint>();
			rotationConstraint.data.constrainedObject = lowerLeg;
			rotationConstraint.data.sourceObjects.Add(new WeightedTransform(kneeTarget, 1f));
			rotationConstraint.data.constrainedXAxis = true;
			rotationConstraint.data.constrainedYAxis = false; // Allow natural knee rotation
			rotationConstraint.data.constrainedZAxis = false;
			rotationConstraint.weight = 0.2f;
		}

		/// <summary>
		/// Génère une contrainte pour le cou
		/// </summary>
		private static void GenerateNeckConstraint(Animator animator, Transform rigContainer, Transform neckTarget) {
			var neckBone = animator.GetBoneTransform(HumanBodyBones.Neck);

			if (!neckBone || !neckTarget) return;

			// Create constraint GameObject
			var constraintGo = new GameObject("NeckConstraint");
			constraintGo.transform.SetParent(rigContainer);

			// Add MultiPosition constraint for neck position
			var positionConstraint = constraintGo.AddComponent<MultiPositionConstraint>();
			positionConstraint.data.constrainedObject = neckBone;
			positionConstraint.data.sourceObjects.Add(new WeightedTransform(neckTarget, 1f));
			positionConstraint.data.constrainedXAxis = true;
			positionConstraint.data.constrainedYAxis = true;
			positionConstraint.data.constrainedZAxis = true;
			positionConstraint.weight = 0.4f;

			// Add MultiRotation constraint for neck rotation
			var rotationConstraint = constraintGo.AddComponent<MultiRotationConstraint>();
			rotationConstraint.data.constrainedObject = neckBone;
			rotationConstraint.data.sourceObjects.Add(new WeightedTransform(neckTarget, 1f));
			rotationConstraint.data.constrainedXAxis = true;
			rotationConstraint.data.constrainedYAxis = true;
			rotationConstraint.data.constrainedZAxis = true;
			rotationConstraint.weight = 0.3f;
		}

		/// <summary>
		/// Génère une contrainte pour la poitrine
		/// </summary>
		private static void GenerateChestConstraint(Animator animator, Transform rigContainer, Transform chestTarget) {
			var chestBone = animator.GetBoneTransform(HumanBodyBones.Chest);

			if (!chestBone || !chestTarget) return;

			// Create constraint GameObject
			var constraintGo = new GameObject("ChestConstraint");
			constraintGo.transform.SetParent(rigContainer);

			// Add MultiPosition constraint for chest position
			var positionConstraint = constraintGo.AddComponent<MultiPositionConstraint>();
			positionConstraint.data.constrainedObject = chestBone;
			positionConstraint.data.sourceObjects.Add(new WeightedTransform(chestTarget, 1f));
			positionConstraint.data.constrainedXAxis = true;
			positionConstraint.data.constrainedYAxis = true;
			positionConstraint.data.constrainedZAxis = true;
			positionConstraint.weight = 0.6f;

			// Add MultiRotation constraint for chest rotation
			var rotationConstraint = constraintGo.AddComponent<MultiRotationConstraint>();
			rotationConstraint.data.constrainedObject = chestBone;
			rotationConstraint.data.sourceObjects.Add(new WeightedTransform(chestTarget, 1f));
			rotationConstraint.data.constrainedXAxis = true;
			rotationConstraint.data.constrainedYAxis = true;
			rotationConstraint.data.constrainedZAxis = true;
			rotationConstraint.weight = 0.5f;
		}

		/// <summary>
		/// Génère une contrainte pour la colonne vertébrale
		/// </summary>
		private static void GenerateSpineConstraint(Animator animator, Transform rigContainer, Transform spineTarget) {
			var spineBone = animator.GetBoneTransform(HumanBodyBones.Spine);

			if (!spineBone || !spineTarget) return;

			// Create constraint GameObject
			var constraintGo = new GameObject("SpineConstraint");
			constraintGo.transform.SetParent(rigContainer);

			// Add MultiPosition constraint for spine position
			var positionConstraint = constraintGo.AddComponent<MultiPositionConstraint>();
			positionConstraint.data.constrainedObject = spineBone;
			positionConstraint.data.sourceObjects.Add(new WeightedTransform(spineTarget, 1f));
			positionConstraint.data.constrainedXAxis = true;
			positionConstraint.data.constrainedYAxis = true;
			positionConstraint.data.constrainedZAxis = true;
			positionConstraint.weight = 0.7f;

			// Add MultiRotation constraint for spine rotation
			var rotationConstraint = constraintGo.AddComponent<MultiRotationConstraint>();
			rotationConstraint.data.constrainedObject = spineBone;
			rotationConstraint.data.sourceObjects.Add(new WeightedTransform(spineTarget, 1f));
			rotationConstraint.data.constrainedXAxis = true;
			rotationConstraint.data.constrainedYAxis = true;
			rotationConstraint.data.constrainedZAxis = true;
			rotationConstraint.weight = 0.6f;
		}

		/// <summary>
		/// Méthode utilitaire pour créer un target
		/// </summary>
		private static Transform CreateTarget(Transform parent, string name, Vector3 position, Quaternion? rotation = null) {
			var targetGo = new GameObject(name);
			targetGo.transform.SetParent(parent);
			targetGo.transform.position = position;
			targetGo.transform.rotation = rotation ?? Quaternion.identity;
			return targetGo.transform;
		}

		/// <summary>
		/// Bascule entre les contraintes TwoBoneIK et LookAt pour la tête
		/// </summary>
		/// <param name="rigContainer">Container du rig</param>
		/// <param name="useHeadTwoBoneIK">True pour TwoBoneIK, false pour LookAt</param>
		public static void SwitchHeadConstraintMode(Transform rigContainer, bool useHeadTwoBoneIK) {
			var headConstraint = rigContainer.Find("HeadConstraint");
			if (!headConstraint) return;

			var ikConstraint = headConstraint.GetComponent<TwoBoneIKConstraint>();
			var aimConstraint = headConstraint.GetComponent<MultiAimConstraint>();

			if (ikConstraint) {
				ikConstraint.weight = useHeadTwoBoneIK ? 1f : 0f;
			}

			if (aimConstraint) {
				aimConstraint.weight = useHeadTwoBoneIK ? 0f : 1f;
			}

			Logger.Log($"Switched head constraint to {(useHeadTwoBoneIK ? "TwoBoneIK" : "LookAt")} mode");
		}
	}
}
