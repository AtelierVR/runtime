using Nox.CCK.Avatars.Rigging.Parameters;
using Nox.CCK.Utils;
using UnityEngine;

#if HAS_FINALIK
using RootMotion.FinalIK;
#endif

namespace Nox.CCK.Avatars.Rigging {
	public static class IKRigParameters {
		public static void SetupParameters(RiggingAvatarModule module) {
			module.Parameters.Clear();

			#if HAS_FINALIK
			// FinalIK VR: paramètres simplifiés pour VRIK
			SetupVRIKParameters(module);
			#else
			// Legacy RigBuilder: paramètres complets pour chaque layer
			SetupRigBuilderParameters(module);
			#endif

			// Paramètres communs pour tous les bones
			for (var i = 0; i < (int)HumanBodyBones.LastBone; i++) {
				var bone = (HumanBodyBones)i;
				if (!module.GetPart(bone)) continue;
				module.Parameters.Add(new RiggingActiveParameter(bone, module));
				module.Parameters.Add(new RiggingPositionParameter(bone, module));
				module.Parameters.Add(new RiggingRotationParameter(bone, module));
			}
		}

		#if HAS_FINALIK
		private static void SetupVRIKParameters(RiggingAvatarModule module) {
			var vrik = module.GetVRIK();
			if (!vrik) return;

			// Paramètres globaux VRIK
			// TODO: Ajouter des paramètres spécifiques pour VRIK si nécessaire
			// Par exemple: spine weights, locomotion settings, etc.
		}
		#else
		private static void SetupRigBuilderParameters(RiggingAvatarModule module) {
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

				// Paramètres spéciaux pour HipsHead (contraintes Multi)
				if (layerName == IKRigGenerator.HipsHead) {
					module.Parameters.Add(new HipConstraintWeightParameter($"rig/ik/{snakeCaseLayerName}/hip_position_weight", HipConstraintWeightParameter.ConstraintType.Position, rigBuilder));
					module.Parameters.Add(new HipConstraintWeightParameter($"rig/ik/{snakeCaseLayerName}/hip_rotation_weight", HipConstraintWeightParameter.ConstraintType.Rotation, rigBuilder));
				}
			}
		}
		#endif
	}
}