using Nox.CCK.Network;
using Nox.Entities;

namespace api.nox.relay {
	public class UndefinedRelayParameter : RelayParameter {
		private readonly int     _hash;
		private          byte[]  _value;
		private          DirtyBy _dirty;

		public UndefinedRelayParameter(RelayEntity entity, int hash, byte[] value) : base(entity) {
			_hash  = hash;
			_value = value;
		}

		public override string GetKey()
			=> _hash.ToString();

		public override object GetValue()
			=> _value;

		public override void SetValue(object value, DirtyBy by) {
			_value = value.ToBytes();
			SetDirty(by);
		}

		public override byte[] Serialize()
			=> _value;

		public override void Deserialize(byte[] data, DirtyBy by) {
			_value = data;
			SetDirty(by);
		}

		public override DirtyBy GetDirty()
			=> _dirty;

		public override void SetDirty(DirtyBy dirtyBy)
			=> _dirty = dirtyBy switch {
				DirtyBy.Local                  => DirtyBy.Local,
				DirtyBy.None or DirtyBy.Remote => DirtyBy.None,
				_                              => throw new System.ArgumentOutOfRangeException(nameof(dirtyBy), dirtyBy, null)
			};

		public override PropertyFlags GetFlags()
			=> PropertyFlags.Synced;
	}
}