using System.Collections.Generic;
using System.Linq;
using Nox.CCK.Network;
using Nox.CCK.Players;
using Nox.Entities;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.relay {
	public abstract class RelayEntity : IEntity {
		protected readonly List<RelayProperty> Properties = new();
		protected readonly List<RelayPart>     Parts      = new();

		public abstract ushort GetId();

		public RelayEntity()
			=> Parts.Add(new RelayBasePart(this));

		#region Properties

		IProperty[] IEntity.GetProperties()
			=> GetProperties<IProperty>().ToArray();

		public T[] GetProperties<T>() where T : IProperty
			=> Properties.OfType<T>().ToArray();

		public T[] GetParts<T>() where T : IPart
			=> Parts.OfType<T>().ToArray();

		public void AddPart(RelayPart part) {
			if (part.GetId() == PlayerRig.Base.ToIndex()) return;
			Parts.Add(part);
		}

		public void RemovePart(ushort id) {
			if (id == PlayerRig.Base.ToIndex()) return;
			Parts.RemoveAll(p => p.GetId() == id);
		}

		bool IEntity.TryGetProperty(string key, out IProperty property)
			=> TryGetProperty(key, out property);

		private bool TryGetProperty<T>(string key, out T property) where T : IProperty {
			var prop = GetProperties<IProperty>().FirstOrDefault(p => p.GetKey() == key);
			if (prop is T pt) {
				property = pt;
				return true;
			}

			property = default;
			return false;
		}

		public void AddProperty(RelayProperty property)
			=> Properties.Add(property);

		public void RemoveProperty(string id)
			=> Properties.RemoveAll(p => p.GetKey() == id);

		#endregion

		#region Moving

		public virtual Vector3 GetPosition()
			=> TryGetPart(PlayerRig.Base.ToIndex(), out var part) && part.TryGetPosition(out var position)
				? position
				: Vector3.zero;

		public virtual void SetPosition(Vector3 position, DirtyBy markDirty) {
			if (!TryGetPart(PlayerRig.Base.ToIndex(), out var part)) {
				Logger.LogWarning($"Base part not found on entity {GetId()}", tag: nameof(RelayEntity));
				return;
			}

			part.SetPosition(position, markDirty);
		}

		public virtual Quaternion GetRotation()
			=> TryGetPart(PlayerRig.Base.ToIndex(), out var part) && part.TryGetRotation(out var rotation)
				? rotation
				: Quaternion.identity;


		public virtual void SetRotation(Quaternion rotation, DirtyBy markDirty) {
			if (!TryGetPart(PlayerRig.Base.ToIndex(), out var part)) {
				Logger.LogWarning($"Base part not found on entity {GetId()}", tag: nameof(RelayEntity));
				return;
			}

			part.SetRotation(rotation, markDirty);
		}

		public virtual Vector3 GetScale()
			=> TryGetPart(PlayerRig.Base.ToIndex(), out var part) && part.TryGetScale(out var scale)
				? scale
				: Vector3.zero;

		public virtual void SetScale(Vector3 scale, DirtyBy markDirty) {
			if (!TryGetPart(PlayerRig.Base.ToIndex(), out var part)) {
				Logger.LogWarning($"Base part not found on entity {GetId()}", tag: nameof(RelayEntity));
				return;
			}

			part.SetScale(scale, markDirty);
		}

		public Vector3 GetVelocity()
			=> TryGetPart(PlayerRig.Base.ToIndex(), out var part) && part.TryGetVelocity(out var velocity)
				? velocity
				: Vector3.zero;

		public virtual void SetVelocity(Vector3 velocity, DirtyBy markDirty) {
			if (!TryGetPart(PlayerRig.Base.ToIndex(), out var part)) {
				Logger.LogWarning($"Base part not found on entity {GetId()}", tag: nameof(RelayEntity));
				return;
			}

			part.SetVelocity(velocity, markDirty);
		}

		public Vector3 GetAngularVelocity()
			=> TryGetPart(PlayerRig.Base.ToIndex(), out var part) && part.TryGetAngularVelocity(out var angularVelocity)
				? angularVelocity
				: Vector3.zero;

		public virtual void SetAngularVelocity(Vector3 angular, DirtyBy markDirty) {
			if (!TryGetPart(PlayerRig.Base.ToIndex(), out var part)) {
				Logger.LogWarning($"Base part not found on entity {GetId()}", tag: nameof(RelayEntity));
				return;
			}

			part.SetAngularVelocity(angular, markDirty);
		}

		#endregion

		#region MultiPart

		public RelayPart[] GetParts()
			=> Parts.ToArray();

		public bool TryGetPart(ushort id, out IPart part) {
			var relayPart = Parts.FirstOrDefault(t => t.GetId() == id);
			if (relayPart != null) {
				part = relayPart;
				return true;
			}

			part = null;
			return false;
		}

		#endregion

		#region Physical

		public abstract bool HasPhysical();

		public abstract bool TryGetPhysical<T>(out T physical) where T : Physical;

		public abstract bool MakePhysical();

		public abstract void DestroyPhysical();

		#endregion
	}
}