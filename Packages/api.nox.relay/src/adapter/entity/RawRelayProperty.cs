using System;
using Nox.CCK.Network;
using Nox.Entities;

namespace api.nox.relay {
	public class RawRelayProperty : RelayProperty {
		private readonly string        _key;
		private          object        _value;
		private          bool          _dirty;
		private          PropertyFlags _flags;

		public RawRelayProperty(RelayPlayer player, string key, object value) : base(player) {
			if (string.IsNullOrEmpty(key))
				throw new ArgumentException("Property key cannot be null or empty", nameof(key));
			_key = key;
			_value = value
				?? throw new ArgumentNullException(nameof(value), "Property value cannot be null");
		}

		public override string GetKey()
			=> _key;

		public override object GetValue()
			=> _value;

		public override PropertyFlags GetFlags()
			=> _flags;

		public override void SetValue(object value)
			=> _value = value;

		public override byte[] Serialize() {
			try {
				return _value.ToBytes();
			} catch (Exception e) {
				throw new InvalidOperationException($"Failed to serialize property '{_key}'", e);
			}
		}

		public override void Deserialize(byte[] data) {
			try {
				_value = data.FromBytes(_value.GetType());
			} catch (Exception e) {
				throw new InvalidOperationException($"Failed to deserialize property '{_key}'", e);
			}
		}

		public override bool IsDirty()
			=> _dirty;

		public override void SetDirty(bool dirty = true)
			=> _dirty = dirty;
	}
}