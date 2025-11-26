using System.Linq;
using Nox.CCK.Avatars.Rigging.Parameters;
using Nox.CCK.Utils;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using Transform = UnityEngine.Transform;

namespace Nox.CCK.Avatars.Rigging {
	public class RigBuilderAvatarModule : BaseRiggingModule {
		private RigBuilder _rig;

		// ReSharper disable Unity.PerformanceAnalysis
		public RigBuilder GetRig()
			=> _rig ??= Descriptor.GetAnchor()?.GetComponent<RigBuilder>();

		public override bool SetupParameters(BaseRiggingModule m) {
			if (m is not RigBuilderAvatarModule module)
				return false;

			var rig = module.GetRig();
			if (!rig || rig.layers == null) return false;

			foreach (var layer in rig.layers) {
				if (!layer.rig) continue;

				var layerName = layer.rig.name;
				if (layerName.StartsWith("RigIK_"))
					layerName = layerName[6..];
				var snakeCaseLayerName = layerName.ToSnakeCase();
				layerName = layer.rig.name;

				module.Parameters.Add(new RigBuilderLayerWeightParameter($"rig/layers/{snakeCaseLayerName}/weight", layerName, rig));
				module.Parameters.Add(new RigBuilderLayerActiveParameter($"rig/layers/{snakeCaseLayerName}/enabled", layerName, rig));

				module.Parameters.Add(new IKWeightParameter($"rig/ik/{snakeCaseLayerName}/position_weight", layerName, IKWeightParameter.WeightType.Position, rig));
				module.Parameters.Add(new IKWeightParameter($"rig/ik/{snakeCaseLayerName}/rotation_weight", layerName, IKWeightParameter.WeightType.Rotation, rig));
				module.Parameters.Add(new IKWeightParameter($"rig/ik/{snakeCaseLayerName}/hint_weight", layerName, IKWeightParameter.WeightType.Hint, rig));
			}

			return true;
		}

		public override bool IsActive(HumanBodyBones bone) {
			var rig = GetRig();
			if (!rig) return false;
			var n = RigBuilderRigGenerator.GetRigFromBone(bone);
			return (from layer in rig.layers
				where layer.rig && layer.rig.name == n
				select layer.active)
				.FirstOrDefault();
		}

		public override void SetActive(HumanBodyBones bone, bool active) {
			var rig = GetRig();
			if (!rig) return;
			var n = RigBuilderRigGenerator.GetRigFromBone(bone);
			foreach (var layer in rig.layers) {
				if (!layer.rig || layer.rig.name != n) continue;
				layer.active = active;
				return;
			}
		}

	public Transform GetOrAddPart(HumanBodyBones bone, Transform tf) {
		var part = GetPart(bone);
		if (part) return part;

		var tr = GetBone(bone);
		if (!tr) return null;			// Use "IK_" prefix to avoid conflicts with Unity's human bone mapping
			var p = new GameObject($"IK_{bone}").transform;
			p.SetParent(tf ?? transform, false);
			p.position   = tr.position;
			p.rotation   = tr.rotation;
			p.localScale = Vector3.one;

			SetPart(bone, p);

			return p;
		}
	}
}