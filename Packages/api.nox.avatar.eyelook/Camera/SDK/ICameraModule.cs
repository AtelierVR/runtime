using UnityEngine;

namespace Nox.Avatars.Camera {
	public interface ICameraModule : IAvatarModule {
		/// <summary>
		/// Get the offset of the camera from the anchor point.
		/// </summary>
		/// <returns></returns>
		public Vector3 GetOffset();

		/// <summary>
		/// Get the Anchor Transform of the camera.
		/// Is recommended to be the head or a similar point of interest.
		/// </summary>
		/// <returns></returns>
		public Transform GetAnchor();
	}
}