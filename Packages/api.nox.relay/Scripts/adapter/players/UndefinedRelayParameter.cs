using System;
using Nox.CCK.Network;
using Nox.Entities;

namespace api.nox.relay {
	public class UndefinedRelayParameter : RelayParameter {
		private readonly int      _hash;
		private          byte[]   _value;
		private          DirtyBy  _dirty;
		private          DateTime _updated;

		public UndefinedRelayParameter(RelayEntity entity, int hash, byte[] value) : base(entity) {
			_hash    = hash;
			_value   = value;
			_updated = DateTime.UtcNow;
		}

		public override int GetKey()
			=> _hash;

		public override string GetName()
			=> $"Undefined_{_hash}";

		public override object GetValue()
			=> _value;

		public override void SetValue(object value, DirtyBy by) {
			_value   = value.ToBytes();
			_updated = DateTime.UtcNow;
			SetDirty(by);
		}

		public override byte[] Serialize()
			=> _value;

		public override void Deserialize(byte[] data, DirtyBy by) {
			_value   = data;
			_updated = DateTime.UtcNow;
			SetDirty(by);
		}

		public override DirtyBy GetDirty()
			=> _dirty;

		public override void SetDirty(DirtyBy dirtyBy) {
			var @new = dirtyBy switch {
				DirtyBy.Local                  => DirtyBy.Local,
				DirtyBy.None or DirtyBy.Remote => DirtyBy.None,
				_                              => throw new System.ArgumentOutOfRangeException(nameof(dirtyBy), dirtyBy, null)
			};
			_dirty   = @new;
			_updated = DateTime.UtcNow;
		}

		public override PropertyFlags GetFlags()
			=> PropertyFlags.Synced;

		public override DateTime GetUpdated()
			=> _updated;
	}
}