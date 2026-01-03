using System;
using System.Linq;
using api.nox.relay.types.Player;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.Avatars.Players;
using Nox.CCK.Network;
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
		public  InstancePlayer Reference;
		public  RelayAdapter   Adapter;
		private AudioClip      _audioClip;

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
			SetPosition(position, DirtyBy.Force);
			SetRotation(rotation, DirtyBy.Force);
			SetVelocity(velocity, DirtyBy.Force);
			SetAngularVelocity(angular, DirtyBy.Force);
		}


		[NoxPublic(NoxAccess.Method)]
		public void Teleport(Transform transform, Rigidbody rb = null) {
			SetPosition(transform.position, DirtyBy.Force);
			SetRotation(transform.rotation, DirtyBy.Force);
			if (!rb) return;
			SetVelocity(rb.linearVelocity, DirtyBy.Force);
			SetAngularVelocity(rb.angularVelocity, DirtyBy.Force);
		}

		public void MovePart(ushort id, Nox.CCK.Utils.TransformObject transform, DirtyBy markDirty = DirtyBy.Local) {
			if (!TryGetPart(id, out var part)) return;

			if (!part.TryGetPosition(out var position) || !transform.IsSamePosition(position))
				part.SetPosition(transform.GetPosition(), markDirty);

			if (!part.TryGetRotation(out var rotation) || !transform.IsSameRotation(rotation))
				part.SetRotation(transform.GetRotation(), markDirty);

			if (!part.TryGetVelocity(out var velocity) || !transform.IsSameVelocity(velocity))
				part.SetVelocity(transform.GetVelocity(), markDirty);

			if (!part.TryGetAngularVelocity(out var angularVelocity) || !transform.IsSameAngularVelocity(angularVelocity))
				part.SetAngularVelocity(transform.GetAngularVelocity(), markDirty);
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
			=> _audioClip;

		public void SetAudio(AudioClip clip) {
			_audioClip = clip;
			// if (TryGetPhysical<RelayPhysicalRemotePlayer>(out var physical))
			// 	physical.SetVoice(clip);
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