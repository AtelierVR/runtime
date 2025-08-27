using UnityEngine;

namespace Nox.Avatars.Rigging {
	public interface IRiggingModule {
		public Transform GetAnchor();

		public Transform GetPart(HumanBodyBones bone);

		public Vector3 GetPartPosition(HumanBodyBones bone);

		public Quaternion GetPartRotation(HumanBodyBones bone);

		public void SetPartPosition(HumanBodyBones bone, Vector3 position);

		public void SetPartRotation(HumanBodyBones bone, Quaternion rotation);
	}
}