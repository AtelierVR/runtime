using System.Linq;
using Nox.Avatars.Rigging;
using UnityEngine;

namespace Nox.CCK.Avatars.Rigging {
	/// <summary>
	/// Extension methods for RiggingAvatarModule to support IK rigging operations
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
			var rigBuilder = riggingModule.GetRigBuilder();
			if (!rigBuilder) return false;
			var name = IKRigGenerator.GetRigFromBone(bone);
			return (from layer in rigBuilder.layers
				where layer.rig && layer.rig.name == name
				select layer.active).FirstOrDefault();
		}

		public static void SetActive(this RiggingAvatarModule riggingModule, HumanBodyBones bone, bool active) {
			var rigBuilder = riggingModule.GetRigBuilder();
			if (!rigBuilder) return;
			var name = IKRigGenerator.GetRigFromBone(bone);
			foreach (var layer in rigBuilder.layers.Where(layer => layer.rig && layer.rig.name == name))
				layer.active = active;
			rigBuilder.Build();
		}
	}
}