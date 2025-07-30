using System.Collections.Generic;
using api.nox.relay;
using api.nox.relay.types.Player;
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
	public abstract class RelayPlayer : IPlayer {
		protected InstancePlayer Reference;
		protected RelayAdapter   Adapter;

		private readonly  Dictionary<string, object>       _properties = new();
		internal readonly Dictionary<ushort, NoxTransform> Transforms  = new();

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
			tr.DeliveryType = TransformDeliveryType.LocalModified;
			tr.SetPosition(position);
			Transforms[PlayerRig.Base.ToIndex()] = tr;
		}

		[NoxPublic(NoxAccess.Method)]
		public void SetRotation(Quaternion rotation) {
			var tr = Transforms.GetValueOrDefault(PlayerRig.Base.ToIndex())
				?? new NoxTransform();
			if (tr.IsSameRotation(rotation, Adapter.Threshold)) return;
			tr.DeliveryType = TransformDeliveryType.LocalModified;
			tr.SetRotation(rotation);
			Transforms[PlayerRig.Base.ToIndex()] = tr;
		}

		[NoxPublic(NoxAccess.Method)]
		public void Teleport(Vector3 position, Quaternion rotation) {
			var tr = Transforms.GetValueOrDefault(PlayerRig.Base.ToIndex())
				?? new NoxTransform();
			if (tr.IsSamePosition(position, Adapter.Threshold) && tr.IsSameRotation(rotation, Adapter.Threshold)) return;
			tr.DeliveryType = TransformDeliveryType.LocalModified;
			tr.SetPosition(position);
			tr.SetRotation(rotation);
			Transforms[PlayerRig.Base.ToIndex()] = tr;
		}

		[NoxPublic(NoxAccess.Method)]
		public void MovePart(ushort part, NoxTransform transform) {
			if (Transforms.TryGetValue(part, out var existing)
			    && existing.IsSamePosition(transform.GetPosition(), Adapter.Threshold)
			    && existing.IsSameRotation(transform.GetRotation(), Adapter.Threshold)
			   ) return;
			transform.DeliveryType = TransformDeliveryType.LocalModified;
			Transforms[part]       = transform;
		}

		[NoxPublic(NoxAccess.Method)]
		public void Teleport(Transform transform) {
			if (!transform) return;
			Teleport(transform.position, transform.rotation);
		}

		public void SetDisplay(string display) {
			Logger.LogWarning($"{nameof(SetDisplay)} is not currently implemented for {GetType().Name}.");
		}

		public bool TryGetPhysical(out Physical physical) {
			Logger.LogWarning($"{nameof(TryGetPhysical)} is not currently implemented for {GetType().Name}.");
			physical = null;
			return false;
		}
	}
}