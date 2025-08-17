namespace Nox.CCK.Avatars.EyeLooks {
	/// <summary>
	/// Represents the placement of the eye look target relative to the avatar.
	/// </summary>
	public enum EyePlacement : byte {
		/// <summary>
		/// The eye look target is placed in a custom position, not specifically left, right, or center.
		/// </summary>
		Other = 0,

		/// <summary>
		/// The eye look target is placed on the left side of the avatar.
		/// </summary>
		Left = 1,

		/// <summary>
		/// The eye look target is placed on the right side of the avatar.
		/// </summary>
		Right = 2,

		/// <summary>
		/// The eye look target is placed at the center of the avatar's face.
		/// Like cyclops, this is used for avatars with a single eye or for centering the gaze.
		/// </summary>
		Center = 3
	}
}