using Nox.Avatars.Rigging;
using UnityEngine;

namespace api.nox.relay {
	public class RelayRigPart : RelayPart {
		private readonly IRigPart _part;

		/// <summary>
		/// Initializes a new instance of the <see cref="RelayPart"/> class.
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="part"></param>
		public RelayRigPart(RelayEntity entity, IRigPart part) : base(entity)
			=> _part = part;

		/// <summary>
		/// Try to get the Transform and Rigidbody of the part.
		/// </summary>
		/// <param name="go"></param>
		/// <param name="rigid"></param>
		/// <returns></returns>
		protected override bool TryGet(out Transform go, out Rigidbody rigid) {
			if (_part == null) {
				go    = null;
				rigid = null;
				return false;
			}

			_part.TryGetTransform(out go, out rigid);
			return go;
		}
		
		/// <summary>
		/// Get the ID of the part.
		/// </summary>
		/// <returns></returns>
		public override ushort GetId()
			=> _part.GetId();
	}
}