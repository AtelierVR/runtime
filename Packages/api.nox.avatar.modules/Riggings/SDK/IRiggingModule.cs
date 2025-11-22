using UnityEngine;

namespace Nox.Avatars.Rigging {
	public interface IRiggingModule : IAvatarModule {
		public bool TryGetPart(ushort id, out IRigPart part);

		public IRigPart[] GetParts();

		public Transform GetBone(HumanBodyBones bone);
	}
}