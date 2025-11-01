using System;
using Nox.CCK.Network;
using Nox.Entities;

namespace api.nox.relay {
	public class RawRelayProperty : RelayProperty {
		private readonly string        _key;
		private          object        _value;
		private          DirtyBy       _dirty;
		private          PropertyFlags _flags;

		public RawRelayProperty(RelayEntity player, string key, int hash, object value) : base(player) {
			if (string.IsNullOrEmpty(key))
				throw new ArgumentException("Property key cannot be null or empty", nameof(key));
			_key  = key;
			_value = value
				?? throw new ArgumentNullException(nameof(value), "Property value cannot be null");
		}

		public override string GetKey()
			=> _key;


		public override object GetValue()
			=> _value;

		public override PropertyFlags GetFlags()
			=> _flags;

		public override void SetValue(object value, DirtyBy by) {
			_value = value;
			SetDirty(by);
		}

		public override byte[] Serialize() {
			try {
				return _value.ToBytes();
			} catch (Exception e) {
				throw new InvalidOperationException($"Failed to serialize property '{_key}'", e);
			}
		}

		public override void Deserialize(byte[] data, DirtyBy by) {
			try {
				_value = data.FromBytes(_value.GetType());
				SetDirty(by);
			} catch (Exception e) {
				throw new InvalidOperationException($"Failed to deserialize property '{_key}'", e);
			}
		}

		public override DirtyBy GetDirty()
			=> _dirty;

		public override void SetDirty(DirtyBy dirty)
			=> _dirty = dirty switch {
				DirtyBy.Local                  => DirtyBy.Local,
				DirtyBy.None or DirtyBy.Remote => DirtyBy.None,
				_                              => throw new ArgumentOutOfRangeException(nameof(dirty), dirty, null)
			};

		public override string ToString()
			=> $"{GetType().Name}[Key={_key}, Value={_value}, Flags={_flags}, Dirty={_dirty}]";
	}
}