using Nox.Avatars;
using Nox.Avatars.Players;
using Nox.CCK.Development;
using Nox.Players;
using UnityEngine;
using Gizmos = Nox.CCK.Development.Gizmos;
using NoxTransform = Nox.CCK.Utils.Transform;

namespace api.nox.relay {
	/// <summary>
	/// Component physique pour les joueurs relay, permettant l'interaction avec le système physique Unity
	/// </summary>
	[Gizmos("relay.physical.player")]
	public abstract class RelayPhysicalPlayer : PlayerPhysical, IPlayerPhysicalAvatar {
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

		public abstract IRuntimeAvatar GetAvatar();

		public abstract void OnMove(ushort part, NoxTransform move);
	}
}