using UnityEngine;

namespace Nox.Avatars.Rigging {
	public interface IRiggingModule {
		public Transform GetAnchor();

		public Transform GetPart(HumanBodyBones bone);

		public Transform GetBone(HumanBodyBones bone);
	}
}