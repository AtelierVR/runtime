using System;
using System.Collections.Generic;
using api.nox.relay.types.Player;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.Avatars.Parameters;
using Nox.Avatars.Players;
using Nox.CCK.Players;
using Nox.CCK.Utils;
using Nox.Entities;
using Nox.Players;
using Nox.Users;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;
using NoxTransform = Nox.CCK.Utils.Transform;
using Transform = UnityEngine.Transform;

namespace api.nox.relay {
	public abstract class RelayPlayer : IPlayer, IPlayerAvatar, IDisposable {
		public InstancePlayer Reference;
		public RelayAdapter   Adapter;

		private readonly  Dictionary<string, object>              _properties = new();
		internal readonly Dictionary<ushort, NoxTransform>        Transforms  = new();
		internal readonly Dictionary<int, (byte[], DeliveryType)> Parameters  = new();

		public void SetReference(InstancePlayer reference, RelayAdapter adapter) {
			Reference = reference;
			Adapter   = adapter;
		}

		public int GetId()
			=> Reference.Id;

		public string GetDisplay()
			=> Reference.Display;

		public virtual bool IsLocal()
			=> false;

		public IUserIdentifier ToIdentifier()
			=> Reference.Identifier;

		public bool IsMaster()
			=> Reference.Flags.HasFlag(InstancePlayerFlags.InstanceMaster);

		public Dictionary<string, object> GetProperties()
			=> _properties;

		public T GetProperty<T>(string key, T defaultValue) where T : struct
			=> _properties.TryGetValue(key, out var value) && value is T typedValue
				? typedValue
				: defaultValue;

		public void SetProperty<T>(string key, T value) where T : struct
			=> _properties[key] = value;

		public void RemoveProperty(string key)
			=> _properties.Remove(key);

		[NoxPublic(NoxAccess.Method)]
		public Vector3 GetPosition()
			=> Transforms.TryGetValue(PlayerRig.Base.ToIndex(), out var transform)
				? transform.GetPosition()
				: Vector3.zero;

		[NoxPublic(NoxAccess.Method)]
		public Quaternion GetRotation()
			=> Transforms.TryGetValue(PlayerRig.Base.ToIndex(), out var transform)
				? transform.GetRotation()
				: Quaternion.identity;

		[NoxPublic(NoxAccess.Method)]
		public void SetPosition(Vector3 position) {
			var tr = Transforms.GetValueOrDefault(PlayerRig.Base.ToIndex())
				?? new NoxTransform();
			if (tr.IsSamePosition(position, Adapter.Threshold)) return;
			tr.DeliveryType = DeliveryType.LocalModified;
			tr.SetPosition(position);
			Transforms[PlayerRig.Base.ToIndex()] = tr;
		}

		[NoxPublic(NoxAccess.Method)]
		public void SetRotation(Quaternion rotation) {
			var tr = Transforms.GetValueOrDefault(PlayerRig.Base.ToIndex())
				?? new NoxTransform();
			if (tr.IsSameRotation(rotation, Adapter.Threshold)) return;
			tr.DeliveryType = DeliveryType.LocalModified;
			tr.SetRotation(rotation);
			Transforms[PlayerRig.Base.ToIndex()] = tr;
		}

		[NoxPublic(NoxAccess.Method)]
		public Vector3 GetVelocity()
			=> Transforms.TryGetValue(PlayerRig.Base.ToIndex(), out var transform)
				? transform.GetVelocity()
				: Vector3.zero;

		[NoxPublic(NoxAccess.Method)]
		public void SetVelocity(Vector3 velocity) {
			var tr = Transforms.GetValueOrDefault(PlayerRig.Base.ToIndex())
				?? new NoxTransform();
			if (tr.IsSameVelocity(velocity, Adapter.Threshold)) return;
			tr.DeliveryType = DeliveryType.LocalModified;
			tr.SetVelocity(velocity);
			Transforms[PlayerRig.Base.ToIndex()] = tr;
		}

		[NoxPublic(NoxAccess.Method)]
		public Vector3 GetAngularVelocity()
			=> Transforms.TryGetValue(PlayerRig.Base.ToIndex(), out var transform)
				? transform.GetAngularVelocity()
				: Vector3.zero;

		[NoxPublic(NoxAccess.Method)]
		public void SetAngularVelocity(Vector3 angular) {
			var tr = Transforms.GetValueOrDefault(PlayerRig.Base.ToIndex())
				?? new NoxTransform();
			if (tr.IsSameAngularVelocity(angular, Adapter.Threshold)) return;
			tr.DeliveryType = DeliveryType.LocalModified;
			tr.SetAngularVelocity(angular);
			Transforms[PlayerRig.Base.ToIndex()] = tr;
		}

		[NoxPublic(NoxAccess.Method)]
		public void Teleport(Vector3 position, Quaternion rotation) {
			var tr = Transforms.GetValueOrDefault(PlayerRig.Base.ToIndex())
				?? new NoxTransform();
			if (tr.IsSamePosition(position, Adapter.Threshold) && tr.IsSameRotation(rotation, Adapter.Threshold)) return;
			tr.DeliveryType = DeliveryType.LocalModified;
			tr.SetPosition(position);
			tr.SetRotation(rotation);
			Transforms[PlayerRig.Base.ToIndex()] = tr;
		}

		[NoxPublic(NoxAccess.Method)]
		public void MovePart(ushort part, NoxTransform transform)
			=> MovePart(part, transform, DeliveryType.LocalModified);

		public void MovePart(ushort part, NoxTransform transform, DeliveryType delivery) {
			if (Transforms.TryGetValue(part, out var existing)
			    && existing.IsSamePosition(transform.GetPosition(), Adapter.Threshold)
			    && existing.IsSameRotation(transform.GetRotation(), Adapter.Threshold)
			   ) return;
			transform.DeliveryType = delivery;
			Transforms[part]       = transform;

			if (delivery != DeliveryType.RemoteModified || !TryGetPhysical<RelayPhysicalPlayer>(out var physical))
				return;

			physical.OnMove(part, transform);
			transform.DeliveryType = DeliveryType.None;
		}

		[NoxPublic(NoxAccess.Method)]
		public void Move(NoxTransform transform)
			=> MovePart(PlayerRig.Base.ToIndex(), transform);

		public void SetParameter(int key, byte[] value, DeliveryType remoteModified) {
			if (Parameters.TryGetValue(key, out var existing)
			    && existing.Item1.Length == value.Length
			    && existing.Item1.AsSpan().SequenceEqual(value)
			   ) return;
			Parameters[key] = (value, remoteModified);

			if (remoteModified != DeliveryType.RemoteModified || !TryGetPhysical<RelayPhysicalPlayer>(out var physical))
				return;

			physical.OnParameter(key, value);
			Parameters[key] = (value, DeliveryType.None);
		}

		[NoxPublic(NoxAccess.Method)]
		public void SetParameter(int key, byte[] value)
			=> SetParameter(key, value, DeliveryType.LocalModified);

		[NoxPublic(NoxAccess.Method)]
		public void Teleport(Transform transform) {
			if (!transform) return;
			Teleport(transform.position, transform.rotation);
		}

		[NoxPublic(NoxAccess.Method)]
		public void SetDisplay(string display) {
			Logger.LogWarning($"{nameof(SetDisplay)} is not currently implemented for {GetType().Name}.");
		}

		public abstract bool TryGetPhysical<T>(out T physical) where T : Physical;

		public abstract bool MakePhysical();

		public abstract void DestroyPhysical();

		public abstract bool HasPhysical();

		public abstract UniTask<bool> SetAvatar(IAvatarIdentifier identifier);

		public abstract IAvatarIdentifier GetAvatar();

		public override string ToString()
			=> $"{GetType().Name}[Id={GetId()}, Display={GetDisplay()}, Identifier={ToIdentifier()}, IsMaster={IsMaster()}]";

		public void Dispose() {
			DestroyPhysical();
			Transforms.Clear();
			_properties.Clear();
			Reference = null;
			Adapter   = null;
		}
	}
}