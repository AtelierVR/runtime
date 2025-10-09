using Nox.CCK.Avatars.Rigging;
using Nox.CCK.Avatars.Rigging.Parameters;
using Nox.CCK.Utils;
using UnityEngine;

public class HeadTarget : MonoBehaviour {
	public RiggingAvatarModule avatar;
	public float               height = 1.8f;

	private void Update() {
		var target = avatar.GetPart(HumanBodyBones.Head);
		var pos    = target.position;
		var origin = avatar.GetRigBuilder().transform;
		origin.position = new Vector3(origin.position.x, pos.y - height, origin.position.z);
		target.position = pos;
	}

	public static void CreateTargets(RiggingAvatarModule module, bool active = false) {
		var rt     = module.GetAnchor().GetOrAddComponent<HeadTarget>();
		var origin = module.GetRigBuilder().transform.position;
		var head   = module.GetBone(HumanBodyBones.Head).position;
		module.Parameters.Add(new HeadTargetParameter("rig/ik/head/target", rt));
		rt.height  = head.y - origin.y;
		rt.avatar  = module;
		rt.enabled = active;
	}
}