using System;
using Nox.CCK.Development;
using Nox.Entities;
using UnityEngine;
using Gizmos = Nox.CCK.Development.Gizmos;

namespace api.nox.relay {
	/// <summary>
	/// Component physique pour les joueurs relay, permettant l'interaction avec le système physique Unity
	/// </summary>
	[Gizmos("relay.physical.player")]
	public class RelayPhysicalPlayer : Physical {
		private RelayPlayer _reference;

		public void SetReference(RelayPlayer player) {
			_reference = player;
		}

		private void OnDrawGizmos() {
			Gizmos.color = Color.green;
			if (_reference == null) return;
			Gizmos.DrawWireSphere(transform.position, 0.5f);
			Gizmos.DrawLine(transform.position, _reference.GetPosition());
		}
	}
}