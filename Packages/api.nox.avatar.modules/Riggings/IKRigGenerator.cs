using Nox.Avatars.Rigging;
using Nox.CCK.Utils;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using Transform = UnityEngine.Transform;

namespace Nox.CCK.Avatars.Rigging {
	/// <summary>
	/// Générateur statique pour les systèmes IK de rigging avatar (Legacy - RigBuilder)
	/// 
	/// Note: GameObject names use "IKRig_" prefix to avoid conflicts with Unity's human bone mapping system.
	/// This prevents ambiguous bone references that can cause avatar validation errors.
	/// 
	/// Ce système est utilisé quand HAS_FINALIK n'est pas défini. 
	/// Quand FinalIK est disponible, préférez utiliser FinalIKRigGenerator.
	/// </summary>
	public static class IKRigGenerator {
		public const string HipsHead   = "IKRig_HipsHead";
		public const string UpperSpine = "IKRig_UpperSpine";
		public const string LeftArm    = "IKRig_LeftArm";
		public const string RightArm   = "IKRig_RightArm";
		public const string LeftLeg    = "IKRig_LeftLeg";
		public const string RightLeg   = "IKRig_RightLeg";
		public const string LeftToe    = "IKRig_LeftToe";
		public const string RightToe   = "IKRig_RightToe";

		public static string GetRigFromBone(HumanBodyBones bone)
			=> bone switch {
				HumanBodyBones.Hips                                                                      => HipsHead,
				HumanBodyBones.Chest or HumanBodyBones.Neck or HumanBodyBones.Head                       => UpperSpine,
				HumanBodyBones.LeftUpperArm or HumanBodyBones.LeftLowerArm or HumanBodyBones.LeftHand    => LeftArm,
				HumanBodyBones.RightUpperArm or HumanBodyBones.RightLowerArm or HumanBodyBones.RightHand => RightArm,
				HumanBodyBones.LeftUpperLeg or HumanBodyBones.LeftLowerLeg or HumanBodyBones.LeftFoot    => LeftLeg,
				HumanBodyBones.RightUpperLeg or HumanBodyBones.RightLowerLeg or HumanBodyBones.RightFoot => RightLeg,
				HumanBodyBones.LeftToes                                                                  => LeftToe,
				HumanBodyBones.RightToes                                                                 => RightToe,
				_                                                                                        => null
			};

		public static HumanBodyBones GetBoneFromRig(string rig)
			=> rig switch {
				HipsHead   => HumanBodyBones.Hips,
				UpperSpine => HumanBodyBones.Chest,
				LeftArm    => HumanBodyBones.LeftHand,
				RightArm   => HumanBodyBones.RightHand,
				LeftLeg    => HumanBodyBones.LeftFoot,
				RightLeg   => HumanBodyBones.RightFoot,
				LeftToe    => HumanBodyBones.LeftToes,
				RightToe   => HumanBodyBones.RightToes,
				_          => HumanBodyBones.LastBone
			};

		public static RigBuilder CreateIKRig(RiggingAvatarModule module) {
			var rigBuilder = CreateRigBuilder(module);
			rigBuilder.enabled = false;

			CreateUpperSpine(module, rigBuilder);
			CreateLeftArm(module, rigBuilder);
			CreateRightArm(module, rigBuilder);
			CreateLeftLeg(module, rigBuilder);
			CreateRightLeg(module, rigBuilder);
			CreateLeftToe(module, rigBuilder);
			CreateRightToe(module, rigBuilder);
			UpdateParts(module);

			rigBuilder.enabled = true;
			return rigBuilder;
		}

		private static RigBuilder CreateRigBuilder(RiggingAvatarModule module) {
			var rigBuilder = module.GetRigBuilder();
			rigBuilder.layers.Clear();
			return rigBuilder;
		}

		private static void UpdateParts(RiggingAvatarModule module) { }

		private static void CreateUpperSpine(RiggingAvatarModule module, RigBuilder rigBuilder) {
			var upperSpine = new GameObject(UpperSpine);
			upperSpine.transform.SetParent(rigBuilder.transform);
			upperSpine.transform.localPosition = Vector3.zero;
			upperSpine.transform.localRotation = Quaternion.identity;
			upperSpine.transform.localScale    = Vector3.one;

			var rig = upperSpine.AddComponent<Rig>();
			rig.weight = 1.0f;
			rigBuilder.layers.Add(new RigLayer(rig));

			var contraint = new GameObject("IK_UpperSpineConstraint");
			contraint.transform.SetParent(upperSpine.transform);
			contraint.transform.localPosition = Vector3.zero;
			contraint.transform.localRotation = Quaternion.identity;
			contraint.transform.localScale    = Vector3.one;
			var constraint = contraint.AddComponent<TwoBoneIKConstraint>();

			constraint.data.root   = module.GetBone(HumanBodyBones.Chest);
			constraint.data.mid    = module.GetBone(HumanBodyBones.Neck);
			constraint.data.tip    = module.GetBone(HumanBodyBones.Head);
			constraint.data.target = module.GetOrAddPart(HumanBodyBones.Head, upperSpine.transform);
			constraint.data.hint   = module.GetOrAddPart(HumanBodyBones.Neck, upperSpine.transform);

			if (constraint.data.root)
				constraint.data.root.gameObject.GetOrAddComponent<RigTransform>();
			if (constraint.data.mid)
				constraint.data.mid.gameObject.GetOrAddComponent<RigTransform>();
			if (constraint.data.tip)
				constraint.data.tip.gameObject.GetOrAddComponent<RigTransform>();

			constraint.data.targetPositionWeight = 1.0f;
			constraint.data.targetRotationWeight = 1.0f;
			constraint.data.hintWeight           = 1.0f;
		}

		private static void CreateLeftArm(RiggingAvatarModule module, RigBuilder rigBuilder)
			=> CreateArm(
				LeftArm,
				HumanBodyBones.LeftUpperArm,
				HumanBodyBones.LeftLowerArm,
				HumanBodyBones.LeftHand,
				module, rigBuilder
			);

		private static void CreateRightArm(RiggingAvatarModule module, RigBuilder rigBuilder)
			=> CreateArm(
				RightArm,
				HumanBodyBones.RightUpperArm,
				HumanBodyBones.RightLowerArm,
				HumanBodyBones.RightHand,
				module, rigBuilder
			);

		private static void CreateArm(string name, HumanBodyBones upperBone, HumanBodyBones lowerBone, HumanBodyBones handBone, RiggingAvatarModule module, RigBuilder rigBuilder) {
			var arm = new GameObject(name);
			arm.transform.SetParent(rigBuilder.transform);
			arm.transform.localPosition = Vector3.zero;
			arm.transform.localRotation = Quaternion.identity;
			arm.transform.localScale    = Vector3.one;

			var rig = arm.AddComponent<Rig>();
			rig.weight = 1.0f;
			rigBuilder.layers.Add(new RigLayer(rig));

			var contraint = new GameObject($"IK_{name}Constraint");
			contraint.transform.SetParent(arm.transform);
			contraint.transform.localPosition = Vector3.zero;
			contraint.transform.localRotation = Quaternion.identity;
			contraint.transform.localScale    = Vector3.one;
			var constraint = contraint.AddComponent<TwoBoneIKConstraint>();

			constraint.data.root   = module.GetBone(upperBone);
			constraint.data.mid    = module.GetBone(lowerBone);
			constraint.data.tip    = module.GetBone(handBone);
			constraint.data.target = module.GetOrAddPart(handBone, arm.transform);
			constraint.data.hint   = module.GetOrAddPart(lowerBone, arm.transform);

			if (constraint.data.root)
				constraint.data.root.gameObject.GetOrAddComponent<RigTransform>();
			if (constraint.data.mid)
				constraint.data.mid.gameObject.GetOrAddComponent<RigTransform>();
			if (constraint.data.tip)
				constraint.data.tip.gameObject.GetOrAddComponent<RigTransform>();

			constraint.data.targetPositionWeight = 1.0f;
			constraint.data.targetRotationWeight = 1.0f;
			constraint.data.hintWeight           = 1.0f;
		}

		private static void CreateLeftLeg(RiggingAvatarModule module, RigBuilder rigBuilder)
			=> CreateLeg(
				LeftLeg,
				HumanBodyBones.LeftUpperLeg,
				HumanBodyBones.LeftLowerLeg,
				HumanBodyBones.LeftFoot,
				module, rigBuilder
			);

		private static void CreateRightLeg(RiggingAvatarModule module, RigBuilder rigBuilder)
			=> CreateLeg(
				RightLeg,
				HumanBodyBones.RightUpperLeg,
				HumanBodyBones.RightLowerLeg,
				HumanBodyBones.RightFoot,
				module, rigBuilder
			);

		private static void CreateLeg(string name, HumanBodyBones upperBone, HumanBodyBones lowerBone, HumanBodyBones footBone, RiggingAvatarModule module, RigBuilder rigBuilder) {
			var leg = new GameObject(name);
			leg.transform.SetParent(rigBuilder.transform);
			leg.transform.localPosition = Vector3.zero;
			leg.transform.localRotation = Quaternion.identity;
			leg.transform.localScale    = Vector3.one;

			var rig = leg.AddComponent<Rig>();
			rig.weight = 1.0f;
			rigBuilder.layers.Add(new RigLayer(rig));

			var contraint = new GameObject($"IK_{name}Constraint");
			contraint.transform.SetParent(leg.transform);
			contraint.transform.localPosition = Vector3.zero;
			contraint.transform.localRotation = Quaternion.identity;
			contraint.transform.localScale    = Vector3.one;
			var constraint = contraint.AddComponent<TwoBoneIKConstraint>();

			constraint.data.root   = module.GetBone(upperBone);
			constraint.data.mid    = module.GetBone(lowerBone);
			constraint.data.tip    = module.GetBone(footBone);
			constraint.data.target = module.GetOrAddPart(footBone, leg.transform);
			constraint.data.hint   = module.GetOrAddPart(lowerBone, leg.transform);

			if (constraint.data.root)
				constraint.data.root.gameObject.GetOrAddComponent<RigTransform>();
			if (constraint.data.mid)
				constraint.data.mid.gameObject.GetOrAddComponent<RigTransform>();
			if (constraint.data.tip)
				constraint.data.tip.gameObject.GetOrAddComponent<RigTransform>();

			constraint.data.targetPositionWeight = 1.0f;
			constraint.data.targetRotationWeight = 1.0f;
			constraint.data.hintWeight           = 1.0f;
		}

		private static void CreateLeftToe(RiggingAvatarModule module, RigBuilder rigBuilder)
			=> CreateToe(
				LeftToe,
				HumanBodyBones.LeftToes,
				module, rigBuilder
			);

		private static void CreateRightToe(RiggingAvatarModule module, RigBuilder rigBuilder)
			=> CreateToe(
				RightToe,
				HumanBodyBones.RightToes,
				module, rigBuilder
			);

		private static void CreateToe(string name, HumanBodyBones toeBone, RiggingAvatarModule module, RigBuilder rigBuilder) {
			var toe = new GameObject(name);
			toe.transform.SetParent(rigBuilder.transform);
			toe.transform.localPosition = Vector3.zero;
			toe.transform.localRotation = Quaternion.identity;
			toe.transform.localScale    = Vector3.one;

			var rig = toe.AddComponent<Rig>();
			rig.weight = 1.0f;
			rigBuilder.layers.Add(new RigLayer(rig));

			var contraint = new GameObject($"IK_{name}Constraint");
			contraint.transform.SetParent(toe.transform);
			contraint.transform.localPosition = Vector3.zero;
			contraint.transform.localRotation = Quaternion.identity;
			contraint.transform.localScale    = Vector3.one;
			var constraint = contraint.AddComponent<DampedTransform>();

			constraint.data.constrainedObject = module.GetBone(toeBone);
			constraint.data.sourceObject      = module.GetOrAddPart(toeBone, toe.transform);

			if (constraint.data.constrainedObject)
				constraint.data.constrainedObject.gameObject.GetOrAddComponent<RigTransform>();

			constraint.data.dampPosition = 0.1f;
			constraint.data.dampRotation = 0.1f;
		}
	}
}