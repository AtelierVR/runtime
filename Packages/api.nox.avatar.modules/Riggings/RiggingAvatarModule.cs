using System.Linq;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.Avatars.Parameters;
using Nox.Avatars.Rigging;
using Nox.CCK.Utils;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using Logger = Nox.CCK.Utils.Logger;
using Transform = UnityEngine.Transform;
using Nox.CCK.Avatars.Rigging.Parameters;

namespace Nox.CCK.Avatars.Rigging {
	public class RiggingAvatarModule : MonoBehaviour, IAvatarModule, IRiggingModule, IParameterGroup {
		private          IAvatarDescriptor _descriptor;
		private readonly List<IParameter>  _parameters = new();

		public GameObject Anchor
			=> _descriptor.GetRoot();

		// ReSharper disable Unity.PerformanceAnalysis
		public RigBuilder GetRigBuilder()
			=> Anchor.GetComponent<RigBuilder>();

		[Header("Rig Settings")]
		public bool generateHeadRig = true;

		public bool generateTwoBoneIK = true;
		public bool generateHipIK     = true;
		public bool useHeadTwoBoneIK  = true;

		[Header("Target Objects")]
		public Transform leftHandTarget;

		public Transform rightHandTarget;
		public Transform leftFootTarget;
		public Transform rightFootTarget;
		public Transform headTarget;
		public Transform hipTarget;

		[Header("Additional VR Tracker Targets")]
		public Transform leftElbowTarget;

		public Transform rightElbowTarget;
		public Transform leftKneeTarget;
		public Transform rightKneeTarget;
		public Transform leftShoulderTarget;
		public Transform rightShoulderTarget;
		public Transform chestTarget;
		public Transform spineTarget;
		public Transform neckTarget;


		public Transform GetAnchor()
			=> Anchor.transform;

		public async UniTask<bool> Setup(IRuntimeAvatar runtimeAvatar) {
			await UniTask.Yield();
			_descriptor = runtimeAvatar.GetDescriptor();
			SetupRigBuilder();
			GenerateAutoRig();
			SetupParameters();
			return true;
		}


		private void SetupRigBuilder() {
			var rigBuilder = Anchor.GetOrAddComponent<RigBuilder>();
			rigBuilder.enabled = false;
			rigBuilder.layers.Clear();
		}

		private void GenerateAutoRig() {
			var animator = _descriptor.GetAnimator();
			if (!animator || !animator.isHuman) {
				Logger.LogWarning("Avatar must have a humanoid Animator to generate rig automatically.");
				return;
			}

			var rigBuilder   = GetRigBuilder();
			var rigContainer = IKRigGenerator.CreateOrGetRigContainer(Anchor.transform);

			if (generateHeadRig) {
				var headRig = IKRigGenerator.CreateRig(rigContainer, "HeadRig");
				IKRigGenerator.GenerateHeadRig(animator, headRig, ref headTarget, ref neckTarget, ref chestTarget, ref spineTarget, useHeadTwoBoneIK);
				rigBuilder.layers.Add(new RigLayer(headRig.GetComponent<Rig>(), true));
			}

			if (generateTwoBoneIK) {
				var leftArmRig  = IKRigGenerator.CreateRig(rigContainer, "LeftArmRig");
				var rightArmRig = IKRigGenerator.CreateRig(rigContainer, "RightArmRig");
				var leftLegRig  = IKRigGenerator.CreateRig(rigContainer, "LeftLegRig");
				var rightLegRig = IKRigGenerator.CreateRig(rigContainer, "RightLegRig");

				IKRigGenerator.GenerateArmIK(animator, leftArmRig, true, ref leftHandTarget, ref leftElbowTarget, ref leftShoulderTarget);
				IKRigGenerator.GenerateArmIK(animator, rightArmRig, false, ref rightHandTarget, ref rightElbowTarget, ref rightShoulderTarget);
				IKRigGenerator.GenerateLegIK(animator, leftLegRig, true, ref leftFootTarget, ref leftKneeTarget);
				IKRigGenerator.GenerateLegIK(animator, rightLegRig, false, ref rightFootTarget, ref rightKneeTarget);

				rigBuilder.layers.Add(new RigLayer(leftArmRig.GetComponent<Rig>(), true));
				rigBuilder.layers.Add(new RigLayer(rightArmRig.GetComponent<Rig>(), true));
				rigBuilder.layers.Add(new RigLayer(leftLegRig.GetComponent<Rig>(), true));
				rigBuilder.layers.Add(new RigLayer(rightLegRig.GetComponent<Rig>(), true));
			}

			if (generateHipIK) {
				var hipRig = IKRigGenerator.CreateRig(rigContainer, "HipRig");
				IKRigGenerator.GenerateHipIK(animator, hipRig, ref hipTarget);
				rigBuilder.layers.Add(new RigLayer(hipRig.GetComponent<Rig>(), true));
			}

			rigBuilder.Build();
			rigBuilder.enabled = true;
		}

		private void SetupParameters() {
			_parameters.Clear();

			var rigBuilder = GetRigBuilder();
			if (!rigBuilder) return;

			if (rigBuilder.layers == null) return;
			foreach (var layer in rigBuilder.layers) {
				if (!layer.rig) continue;
				var layerName          = layer.rig.name;
				var snakeCaseLayerName = layerName.ToSnakeCase();
				if (snakeCaseLayerName.EndsWith("_rig"))
					snakeCaseLayerName = snakeCaseLayerName[..^4];
				_parameters.Add(new RigBuilderLayerWeightParameter($"rig/layers/{snakeCaseLayerName}/weight", layerName, rigBuilder));
				_parameters.Add(new RigBuilderLayerActiveParameter($"rig/layers/{snakeCaseLayerName}/enabled", layerName, rigBuilder));

				_parameters.Add(new IKWeightParameter($"rig/ik/{snakeCaseLayerName}/position_weight", layerName, IKWeightParameter.WeightType.Position, rigBuilder));
				_parameters.Add(new IKWeightParameter($"rig/ik/{snakeCaseLayerName}/rotation_weight", layerName, IKWeightParameter.WeightType.Rotation, rigBuilder));
				_parameters.Add(new IKWeightParameter($"rig/ik/{snakeCaseLayerName}/hint_weight", layerName, IKWeightParameter.WeightType.Hint, rigBuilder));
			}

			for (var i = 0; i < (int)HumanBodyBones.LastBone; i++) {
				var bone = (HumanBodyBones)i;
				if (!GetPart(bone)) continue;
				_parameters.Add(new RiggingActiveParameter(bone, this));
				_parameters.Add(new RiggingPositionParameter(bone, this));
				_parameters.Add(new RiggingRotationParameter(bone, this));
			}
		}

		public Transform GetPart(HumanBodyBones bone)
			=> bone switch {
				HumanBodyBones.LeftHand      => leftHandTarget,
				HumanBodyBones.RightHand     => rightHandTarget,
				HumanBodyBones.LeftFoot      => leftFootTarget,
				HumanBodyBones.RightFoot     => rightFootTarget,
				HumanBodyBones.Head          => headTarget,
				HumanBodyBones.Hips          => hipTarget,
				HumanBodyBones.LeftUpperArm  => leftElbowTarget,
				HumanBodyBones.RightUpperArm => rightElbowTarget,
				HumanBodyBones.LeftUpperLeg  => leftKneeTarget,
				HumanBodyBones.RightUpperLeg => rightKneeTarget,
				_                            => null
			};

		public Vector3 GetPartPosition(HumanBodyBones bone)
			=> GetPart(bone)?.position ?? Vector3.zero;

		public Quaternion GetPartRotation(HumanBodyBones bone)
			=> GetPart(bone)?.rotation ?? Quaternion.identity;

		public void SetPartPosition(HumanBodyBones bone, Vector3 position) {
			var target = GetPart(bone);
			if (target)
				target.position = position;
		}

		public void SetPartRotation(HumanBodyBones bone, Quaternion rotation) {
			var target = GetPart(bone);
			if (target)
				target.rotation = rotation;
		}

		public bool IsActive(HumanBodyBones bone) {
			var rigBuilder = GetRigBuilder();
			if (!rigBuilder)
				return false;

			var select = bone switch {
				// layer, (0 = target, 1 = hint)
				HumanBodyBones.LeftHand      => ("LeftArmRig", 0),
				HumanBodyBones.RightHand     => ("RightArmRig", 0),
				HumanBodyBones.Head          => ("HeadRig", 0),
				HumanBodyBones.Hips          => ("HipRig", 0),
				HumanBodyBones.LeftFoot      => ("LeftLegRig", 0),
				HumanBodyBones.RightFoot     => ("RightLegRig", 0),
				HumanBodyBones.LeftUpperArm  => ("LeftArmRig", 1),
				HumanBodyBones.RightUpperArm => ("RightArmRig", 1),
				HumanBodyBones.LeftUpperLeg  => ("LeftLegRig", 1),
				HumanBodyBones.RightUpperLeg => ("RightLegRig", 1),
				_                            => (null, -1)
			};

			if (select.Item1 == null || select.Item2 < 0)
				return false;

			foreach (var layer in rigBuilder.layers) {
				if (layer.rig.name != select.Item1) continue;

				var ikConstraints = layer.rig.GetComponentsInChildren<TwoBoneIKConstraint>();
				if (ikConstraints.Length <= select.Item2) continue;

				var constraint = ikConstraints[select.Item2];

				if (select.Item2 == 0)
					return constraint.data.targetPositionWeight > 0f || constraint.data.targetRotationWeight > 0f;

				return constraint.data.hintWeight > 0f;
			}

			return false;
		}

		// ReSharper disable Unity.PerformanceAnalysis
		public void SetActive(HumanBodyBones bone, bool active) {
			var rigBuilder = GetRigBuilder();
			if (!rigBuilder)
				return;

			var select = bone switch {
				HumanBodyBones.LeftHand      => ("LeftArmRig", 0),
				HumanBodyBones.RightHand     => ("RightArmRig", 0),
				HumanBodyBones.Head          => ("HeadRig", 0),
				HumanBodyBones.Hips          => ("HipRig", 0),
				HumanBodyBones.LeftFoot      => ("LeftLegRig", 0),
				HumanBodyBones.RightFoot     => ("RightLegRig", 0),
				HumanBodyBones.LeftUpperArm  => ("LeftArmRig", 1),
				HumanBodyBones.RightUpperArm => ("RightArmRig", 1),
				HumanBodyBones.LeftUpperLeg  => ("LeftLegRig", 1),
				HumanBodyBones.RightUpperLeg => ("RightLegRig", 1),
				_                            => (null, -1)
			};

			if (select.Item1 == null || select.Item2 < 0)
				return;

			foreach (var layer in rigBuilder.layers) {
				if (layer.rig.name != select.Item1) continue;

				var ikConstraints = layer.rig.GetComponentsInChildren<TwoBoneIKConstraint>();
				if (ikConstraints.Length <= select.Item2) continue;

				var constraint = ikConstraints[select.Item2];
				var data       = constraint.data;

				if (select.Item2 == 0) {
					data.targetPositionWeight = active ? 1f : 0f;
					data.targetRotationWeight = active ? 1f : 0f;
				}

				data.hintWeight = active ? 1f : 0f;

				constraint.data = data;
			}
		}

		public IParameter[] GetParameters()
			=> _parameters.Cast<IParameter>().ToArray();

		public IParameter GetParameter(string key)
			=> _parameters.FirstOrDefault(p => p.GetName() == key);

		public IParameter GetParameter(int hash)
			=> _parameters.FirstOrDefault(p => p.GetHash() == hash);

		private void OnDrawGizmosSelected() {
			if (_descriptor == null) return;

			// Cache colors for performance
			var handColor       = Color.cyan;
			var footColor       = Color.yellow;
			var headColor       = Color.green;
			var hipColor        = Color.magenta;
			var elbowColor      = Color.red;
			var kneeColor       = Color.blue;
			var shoulderColor   = Color.orange;
			var torsoColor      = Color.gray;
			var connectionColor = Color.white * 0.3f;

			// Draw hand targets with optimized method
			DrawTargetSphere(leftHandTarget, handColor, 0.05f);
			DrawTargetSphere(rightHandTarget, handColor, 0.05f);

			// Draw foot targets with optimized method
			DrawTargetCube(leftFootTarget, footColor, Vector3.one  * 0.08f, Vector3.up);
			DrawTargetCube(rightFootTarget, footColor, Vector3.one * 0.08f, Vector3.up);

			// Draw head target
			DrawTargetSphere(headTarget, headColor, 0.08f, 0.15f);

			// Draw hip target
			DrawTargetCube(hipTarget, hipColor, Vector3.one * 0.1f, Vector3.forward, 0.12f);

			// Draw elbow targets
			DrawTargetCube(leftElbowTarget, elbowColor, Vector3.one  * 0.04f, Vector3.right, 0.08f);
			DrawTargetCube(rightElbowTarget, elbowColor, Vector3.one * 0.04f, Vector3.right, 0.08f);

			// Draw knee targets
			DrawTargetCube(leftKneeTarget, kneeColor, Vector3.one  * 0.06f, Vector3.forward);
			DrawTargetCube(rightKneeTarget, kneeColor, Vector3.one * 0.06f, Vector3.forward);

			// Draw shoulder targets
			DrawTargetSphere(leftShoulderTarget, shoulderColor, 0.06f, 0.1f, Vector3.up);
			DrawTargetSphere(rightShoulderTarget, shoulderColor, 0.06f, 0.1f, Vector3.up);

			// Draw torso targets
			DrawTargetCube(chestTarget, torsoColor, new Vector3(0.15f, 0.08f, 0.08f), Vector3.forward, 0.12f);
			DrawTargetCube(spineTarget, torsoColor, new Vector3(0.12f, 0.06f, 0.06f), Vector3.forward);
			DrawTargetSphere(neckTarget, torsoColor, 0.04f, 0.08f, Vector3.up);

			// Draw connections with cached color
			Gizmos.color = connectionColor;
			DrawConnections();
		}

		private void DrawConnections() {
			// Arm connections (shoulder -> elbow -> hand)
			DrawConnection(leftShoulderTarget, leftElbowTarget);
			DrawConnection(rightShoulderTarget, rightElbowTarget);
			DrawConnection(leftElbowTarget, leftHandTarget);
			DrawConnection(rightElbowTarget, rightHandTarget);

			// Leg connections (knee -> foot)
			DrawConnection(leftKneeTarget, leftFootTarget);
			DrawConnection(rightKneeTarget, rightFootTarget);

			// Spine hierarchy connections
			DrawConnection(spineTarget, chestTarget);
			DrawConnection(chestTarget, neckTarget);
			DrawConnection(neckTarget, headTarget);
		}

		private static void DrawConnection(Transform from, Transform to) {
			if (!from || !to) return;
			Gizmos.DrawLine(from.position, to.position);
		}

		private static void DrawTargetSphere(Transform target, Color color, float radius, float lineLength = 0.1f, Vector3? lineDirection = null) {
			if (!target) return;
			Gizmos.color = color;
			Gizmos.DrawWireSphere(target.position, radius);
			var direction = lineDirection ?? target.forward;
			Gizmos.DrawLine(target.position, target.position + direction * lineLength);
		}

		private static void DrawTargetCube(Transform target, Color color, Vector3 size, Vector3? lineDirection = null, float lineLength = 0.1f) {
			if (!target) return;
			Gizmos.color = color;
			Gizmos.DrawWireCube(target.position, size);
			if (!lineDirection.HasValue) return;
			var direction = lineDirection.Value;
			Gizmos.DrawLine(target.position, target.position + direction * lineLength);
		}

		public static bool Check(IAvatarDescriptor descriptor) {
			var modules = descriptor.GetModules<RiggingAvatarModule>();

			var module = modules.Length switch {
				1 => modules.FirstOrDefault(),
				0 => descriptor.GetRoot().AddComponent<RiggingAvatarModule>(),
				_ => null
			};

			if (!module) {
				Logger.LogError("Verify that the Avatar prefab has a valid RiggingAvatarModule component.");
				return false;
			}

			return true;
		}
	}
}