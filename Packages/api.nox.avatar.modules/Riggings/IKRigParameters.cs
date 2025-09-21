using Nox.CCK.Avatars.Rigging.Parameters;
using Nox.CCK.Utils;
using UnityEngine;

namespace Nox.CCK.Avatars.Rigging {
	public static class IKRigParameters {
		public static void SetupParameters(RiggingAvatarModule module) {
			module.Parameters.Clear();

			var rigBuilder = module.GetRigBuilder();
			if (!rigBuilder) return;

			if (rigBuilder.layers == null) return;
			foreach (var layer in rigBuilder.layers) {
				if (!layer.rig) continue;

				var layerName = layer.rig.name;
				if (layerName.StartsWith("IKRig_"))
					layerName = layerName[6..];
				var snakeCaseLayerName = layerName.ToSnakeCase();
				layerName = layer.rig.name;

				module.Parameters.Add(new RigBuilderLayerWeightParameter($"rig/layers/{snakeCaseLayerName}/weight", layerName, rigBuilder));
				module.Parameters.Add(new RigBuilderLayerActiveParameter($"rig/layers/{snakeCaseLayerName}/enabled", layerName, rigBuilder));

				module.Parameters.Add(new IKWeightParameter($"rig/ik/{snakeCaseLayerName}/position_weight", layerName, IKWeightParameter.WeightType.Position, rigBuilder));
				module.Parameters.Add(new IKWeightParameter($"rig/ik/{snakeCaseLayerName}/rotation_weight", layerName, IKWeightParameter.WeightType.Rotation, rigBuilder));
				module.Parameters.Add(new IKWeightParameter($"rig/ik/{snakeCaseLayerName}/hint_weight", layerName, IKWeightParameter.WeightType.Hint, rigBuilder));
			}

			for (var i = 0; i < (int)HumanBodyBones.LastBone; i++) {
				var bone = (HumanBodyBones)i;
				if (!module.GetPart(bone)) continue;
				module.Parameters.Add(new RiggingActiveParameter(bone, module));
				module.Parameters.Add(new RiggingPositionParameter(bone, module));
				module.Parameters.Add(new RiggingRotationParameter(bone, module));
			}
		}
	}
}