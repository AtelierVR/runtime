using Nox.CCK.Players;
using UnityEngine;

namespace api.nox.relay {
	public class RelayBasePart : RelayPart {
		public RelayBasePart(RelayEntity entity) : base(entity) { }

		public override ushort GetId()
			=> PlayerRig.Base.ToIndex();

		// ReSharper disable Unity.PerformanceAnalysis
		protected override bool TryGet(out Transform go, out Rigidbody rigid) {
			if (!Entity.TryGetPhysical<RelayPhysicalEntity>(out var physical)) {
				go    = null;
				rigid = null;
				return false;
			}

			go    = physical.transform;
			rigid = physical.GetComponent<Rigidbody>();
			return go;
		}
	}
}