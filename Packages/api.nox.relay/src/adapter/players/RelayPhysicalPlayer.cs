using System.Linq;
using Nox.Avatars;
using Nox.Avatars.Parameters;
using Nox.Avatars.Players;
using Nox.Avatars.Rigging;
using Nox.CCK.Development;
using Nox.CCK.Players;
using UnityEngine;
using Gizmos = Nox.CCK.Development.Gizmos;
using Logger = Nox.CCK.Utils.Logger;
using NoxTransform = Nox.CCK.Utils.Transform;

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

		public abstract void OnMove(ushort part, NoxTransform move);

		public abstract void OnParameter(int key, byte[] value);

		public abstract void SetVoice(AudioClip clip);

		public override bool TryGetPart(ushort id, out GameObject go) {
			var avatar = GetAvatar();
			if (avatar == null) {
				go = null;
				return false;
			}

			var module = avatar.GetDescriptor()
				.GetModules<IRiggingModule>()
				.FirstOrDefault();
			if (module == null) {
				go = null;
				return false;
			}

			var part = module.GetPart(id.ToHumanBodyBones());
			if (!part) {
				go = null;
				return false;
			}

			go = part.gameObject;
			return true;
		}
	}
}