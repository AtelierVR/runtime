using System;
using System.Linq;
using api.nox.relay.types.Player;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.Avatars.Players;
using Nox.CCK.Players;
using Nox.CCK.Utils;
using Nox.Entities;
using Nox.Players;
using Nox.Users;
using Nox.Worlds.Spawns;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;
using Transform = UnityEngine.Transform;

namespace api.nox.relay {
	public abstract class RelayPlayer : RelayEntity, IPlayer, IPlayerAvatar, IDisposable, IAudioEntity {
		public InstancePlayer Reference;
		public RelayAdapter   Adapter;
		public AudioClip      AudioClip;

		public void SetReference(InstancePlayer reference, RelayAdapter adapter) {
			Reference = reference;
			Adapter   = adapter;
		}

		public override ushort GetId()
			=> Reference.Id;

		public string GetDisplay()
			=> Reference.Display;

		public virtual bool IsLocal()
			=> false;

		public IUserIdentifier ToIdentifier()
			=> Reference.Identifier;

		public bool IsMaster()
			=> Reference.Flags.HasFlag(InstancePlayerFlags.InstanceMaster);

		[NoxPublic(NoxAccess.Method)]
		public void Teleport(Vector3 position, Quaternion rotation)
			=> Teleport(position, rotation, Vector3.zero, Vector3.zero);

		public void Teleport(Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angular) {
			if (!TryGetPart(PlayerRig.Base.ToIndex(), out var part)) return;
			part.SetPosition(position);
			part.SetRotation(rotation);
			part.SetVelocity(velocity);
			part.SetAngularVelocity(angular);
		}


		[NoxPublic(NoxAccess.Method)]
		public void Teleport(Transform transform, Rigidbody rb = null) {
			if (!transform || !TryGetPart(PlayerRig.Base.ToIndex(), out var part)) return;
			part.SetPosition(transform.position);
			part.SetRotation(transform.rotation);
			if (!rb) return;
			part.SetVelocity(rb.linearVelocity);
			part.SetAngularVelocity(rb.angularVelocity);
		}

		public void MovePart(ushort id, Nox.CCK.Utils.Transform transform) {
			if (!TryGetPart(id, out var part)) return;
			if (!part.TryGetPosition(out var position) || !transform.IsSamePosition(position))
				part.SetPosition(transform.GetPosition(), true);
			if (!part.TryGetRotation(out var rotation) || !transform.IsSameRotation(rotation))
				part.SetRotation(transform.GetRotation(), true);
			if (!part.TryGetVelocity(out var velocity) || !transform.IsSameVelocity(velocity))
				part.SetVelocity(transform.GetVelocity(), true);
			if (!part.TryGetAngularVelocity(out var angularVelocity) || !transform.IsSameAngularVelocity(angularVelocity))
				part.SetAngularVelocity(transform.GetAngularVelocity(), true);
		}

		[NoxPublic(NoxAccess.Method)]
		public void SetDisplay(string display) {
			if (Reference == null) {
				Logger.LogWarning($"Cannot set display name: Reference is null for {GetType().Name}");
				return;
			}

			if (string.Equals(Reference.Display, display, StringComparison.Ordinal)) {
				return; // No change needed
			}

			Reference.Display = display;
			Logger.LogDebug($"Updated display name for player {GetId()}: {display}");
		}

		public abstract UniTask<bool> SetAvatar(IAvatarIdentifier identifier);

		public abstract IAvatarIdentifier GetAvatar();

		public void Respawn() {
			var dimension   = Adapter.GetDimension();
			var main        = dimension.GetScene().GetInstances()[0];
			var descriptor  = main.GetDescriptor(dimension.GetMainIndex());
			var spawnModule = descriptor?.GetModules<ISpawnModule>().FirstOrDefault();
			if (spawnModule == null) return;
			var spawn = spawnModule.ChoiceSpawn();
			Teleport(spawn.GetPosition(), spawn.GetRotation());
			Logger.LogDebug($"Player {GetId()} respawned at {spawn.GetPosition()}");
		}

		public override string ToString()
			=> $"{GetType().Name}[Id={GetId()}, Display={GetDisplay()}, Identifier={ToIdentifier()}, IsMaster={IsMaster()}]";

		public AudioClip GetAudio()
			=> AudioClip;

		public void SetAudio(AudioClip clip) {
			AudioClip = clip;
			if (TryGetPhysical<RelayPhysicalRemotePlayer>(out var physical))
				physical.SetVoice(clip);
		}

		public void Dispose() {
			DestroyPhysical();
			Parts.Clear();
			Reference = null;
			Adapter   = null;
		}

		IPart[] IMultiPartEntity.GetParts()
			=> GetParts().Cast<IPart>().ToArray();
	}
}