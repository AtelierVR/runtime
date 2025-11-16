using Nox.CCK.Avatars.Rigging;
using Nox.CCK.Avatars.Rigging.Parameters;
using Nox.CCK.Utils;
using UnityEngine;

#if HAS_FINALIK
using RootMotion.FinalIK;
#endif

public class HeadTarget : MonoBehaviour {
	public RiggingAvatarModule avatar;
	public float               height = 1.8f;

	private void Update() {
		var target = avatar.GetPart(HumanBodyBones.Head);
		var pos    = target.position;

		#if HAS_FINALIK
		// FinalIK: utilise le transform du VRIK
		var origin = avatar.GetVrik().transform;
		#else
		// Legacy: utilise le transform du RigBuilder
		var origin = avatar.GetRigBuilder().transform;
		#endif

		origin.position = new Vector3(origin.position.x, pos.y - height, origin.position.z);
		target.position = pos;
	}

	public static void CreateTargets(RiggingAvatarModule module, bool active = false) {
		var rt = module.GetAnchor().GetOrAddComponent<HeadTarget>();

		#if HAS_FINALIK
		// FinalIK: utilise le transform du VRIK
		var origin = module.GetVrik().transform.position;
		#else
		// Legacy: utilise le transform du RigBuilder
		var origin = module.GetRigBuilder().transform.position;
		#endif

		var head = module.GetBone(HumanBodyBones.Head).position;
		module.Parameters.Add(new HeadTargetParameter("rig/ik/head/target", rt));
		rt.height  = head.y - origin.y;
		rt.avatar  = module;
		rt.enabled = active;
	}
}