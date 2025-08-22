using Nox.Avatars;
using Nox.CCK.Development;
using Nox.Players;
using UnityEngine;
using Gizmos = Nox.CCK.Development.Gizmos;

namespace api.nox.relay {
	/// <summary>
	/// Component physique pour les joueurs relay, permettant l'interaction avec le système physique Unity
	/// </summary>
	[Gizmos("relay.physical.player")]
	public class RelayPhysicalPlayer : PlayerPhysical {
		protected RelayPlayer Reference;

		public void SetReference(RelayPlayer player) {
			Reference = player;
		}

		private void OnDrawGizmos() {
			Gizmos.color = Color.green;
			if (Reference == null) return;
			Gizmos.DrawWireSphere(transform.position, 0.5f);
			Gizmos.DrawLine(transform.position, Reference.GetPosition());
		}

		public override IRuntimeAvatar GetAvatar()
			=> null;
	}
}