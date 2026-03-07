using System;
using Nox.CCK.Network;
using UnityEngine;

namespace Nox.Entities {
	/// <summary>
	/// Represents a part of a multi-part entity.
	/// </summary>
	public interface IPart : IDirty {
		/// <summary>
		/// Gets the unique ID of this part within its parent entity.
		/// </summary>
		/// <returns></returns>
		public ushort Id { get; }

		/// <summary>
		/// Gets or sets the position of this part relative to its parent entity.
		/// </summary>
		public Vector3 Position { get; set; }

		/// <summary>
		/// Gets or sets the rotation of this part relative to its parent entity.
		/// </summary>
		public Quaternion Rotation { get; set; }

		/// <summary>
		/// Gets or sets the scale of this part.
		/// </summary>
		public Vector3 Scale { get; set; }

		/// <summary>
		/// Gets or sets the velocity of this part.
		/// </summary>
		public Vector3 Velocity { get; set; }

		/// <summary>
		/// Gets or sets the angular velocity of this part.
		/// </summary>
		public Vector3 Angular { get; set; }

		/// <summary>
		/// Gets the timestamp of the last update to this part.
		/// </summary>
		public DateTime Updated { get; }
	}
}