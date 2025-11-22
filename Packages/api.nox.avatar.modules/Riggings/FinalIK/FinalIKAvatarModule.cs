#if HAS_FINALIK
using RootMotion.FinalIK;
using UnityEngine;

namespace Nox.CCK.Avatars.Rigging {
	public class FinalIKAvatarModule : BaseRiggingModule {
		private VRIK _rig;

		public VRIK GetRig()
			=> _rig ??= Descriptor.GetAnchor()?.GetComponent<VRIK>();

		public override bool SetupParameters(BaseRiggingModule m) {
			if (m is not FinalIKAvatarModule module)
				return false;

			var rig = module.GetRig();

			return true;
		}

		public override bool IsActive(HumanBodyBones bone) {
			return false;
		}

		public override void SetActive(HumanBodyBones bone, bool active) { }
	}
}
#endif