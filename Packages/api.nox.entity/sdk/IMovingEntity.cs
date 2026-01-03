using Nox.CCK.Network;
using UnityEngine;

namespace Nox.Entities {
	/// <summary>
	/// Interface for moving entities.
	/// </summary>
	public interface IMovingEntity : IEntity {
		/// <summary>
		/// Indicates whether the entity has been modified since the last sync.
		/// </summary>
		public Vector3 Position { get; set; }
		
		/// <summary>
		/// Gets or sets the rotation of the entity.
		/// </summary>
		public Quaternion Rotation { get; set; }
		
		/// <summary>
		/// Gets or sets the scale of the entity.
		/// </summary>
		public Vector3 Scale { get; set; }
		
		/// <summary>
		/// Gets or sets the velocity of the entity.
		/// </summary>
		public Vector3 Velocity { get; set; }
		
		/// <summary>
		/// Gets or sets the angular velocity of the entity.
		/// </summary>
		public Vector3 Angular { get; set; }
		
	}
}