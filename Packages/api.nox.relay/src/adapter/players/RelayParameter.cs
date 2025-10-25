using System;
using Nox.Avatars.Parameters;
using Nox.CCK.Utils;
using Nox.Entities;

namespace api.nox.relay {
	public class RelayParameter : RelayProperty {
		private IParameter    _reference;
		private byte[]        _lastSerialValue;
		private object        _lastValue;
		private string        _name;
		private PropertyFlags _flags;

		public RelayParameter(RelayPlayer player, IParameter parameter) : base(player) {
			_reference       = parameter;
			_lastValue       = parameter.Get();
			_lastSerialValue = parameter.Serialize();
			_name            = parameter.GetName();
			_flags = PropertyFlags.None
				| (_reference.IsSyncable() ? PropertyFlags.Synced : PropertyFlags.None)
				| (_reference.IsSavable() ? PropertyFlags.Persistent : PropertyFlags.None);
		}

		public IParameter GetReference()
			=> _reference;

		public void Attach(IParameter parameter) {
			if (parameter == null)
				throw new ArgumentNullException(nameof(parameter), "Cannot attach null parameter.");
			if (_reference == parameter) return;
			if (_reference != null && parameter.GetName() != _reference.GetName())
				throw new InvalidOperationException("Cannot attach parameter with different name.");
			if (_lastValue != null)
				parameter.Set(_lastValue);
			_reference       = parameter;
			_lastValue       = parameter.Get();
			_lastSerialValue = parameter.Serialize();
			_name            = parameter.GetName();
			_flags = PropertyFlags.None
				| (_reference.IsSyncable() ? PropertyFlags.Synced : PropertyFlags.None)
				| (_reference.IsSavable() ? PropertyFlags.Persistent : PropertyFlags.None);
		}

		public override string GetKey()
			=> _name;

		public override bool IsDirty() {
			if (!_reference?.IsValid() ?? true) {
				Logger.LogDebug($"Parameter '{_name}' is invalid {Owner.GetDisplay()}.", tag: nameof(RelayParameter));
				return false;
			}

			return (_reference?.IsValid() ?? false)
				&& !Equals(_lastValue, GetValue());
		}


		public override void SetDirty(bool dirty = true) {
			if (dirty || _reference == null) return;
			_lastValue       = _reference.Get();
			_lastSerialValue = _reference.Serialize();
		}

		public override byte[] Serialize()
			=> _reference != null
				? _reference.Serialize()
				: _lastSerialValue;

		public override void Deserialize(byte[] data) {
			if (_reference == null)
				_lastSerialValue = data;
			else _reference.Deserialize(data);
		}

		public override object GetValue()
			=> _reference != null
				? _reference.Get()
				: _lastValue;

		public override PropertyFlags GetFlags()
			=> _flags;

		public override void SetValue(object value) {
			if (_reference != null)
				_reference.Set(value);
			else _lastValue = value;
		}

		public void Detach() {
			if (_reference == null) return;
			_lastValue       = _reference.Get();
			_lastSerialValue = _reference.Serialize();
			_reference       = null;
		}
	}
}