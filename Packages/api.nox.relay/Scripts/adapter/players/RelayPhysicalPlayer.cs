using System.Linq;
using Nox.Avatars;
using Nox.Avatars.Players;
using Nox.Avatars.Rigging;
using Nox.CCK.Development;
using Nox.CCK.Players;
using UnityEngine;
using Gizmos = Nox.CCK.Development.Gizmos;

namespace api.nox.relay {
	/// <summary>
	/// Component physique pour les joueurs relay, permettant l'interaction avec le système physique Unity
	/// </summary>
	[Gizmos("relay.physical.player")]
	public abstract class RelayPhysicalPlayer : RelayPhysicalEntity, IPlayerPhysicalAvatar {
		protected RelayPlayer Reference;

		public void SetReference(RelayPlayer player)
			=> Reference = player;

		private void OnDrawGizmos() {
			Gizmos.color = Color.green;
			if (Reference == null) return;
			Gizmos.DrawWireSphere(transform.position, 0.5f);
			Gizmos.DrawLine(transform.position, Reference.GetPosition());
		}

		public abstract IRuntimeAvatar GetAvatar();

		public override bool TryGetPart(ushort id, out Transform go, out Rigidbody rigid) {
			var avatar = GetAvatar();
			if (avatar == null) {
				go    = null;
				rigid = null;
				return false;
			}

			var module = avatar.GetDescriptor()
				.GetModules<IRiggingModule>()
				.FirstOrDefault();
			
			if (module == null) {
				go    = null;
				rigid = null;
				return false;
			}

			if (!module.TryGetPart(id, out var part)) {
				go    = null;
				rigid = null;
				return false;
			}

			go    = part.GetTransform();
			rigid = part.GetRigidbody();
			return true;
		}
	}
}