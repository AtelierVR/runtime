using System;
using Nox.Avatars.Parameters;
using Nox.Entities;

namespace api.nox.relay {
	public class RelayParameter : RelayProperty {
		private readonly IParameter _reference;
		private          object     _lastValue;

		public RelayParameter(IParameter parameter) {
			_reference = parameter;
			_lastValue = parameter.Get();
		}

		public void Attach(IParameter parameter) {
			if (parameter == null)
				throw new ArgumentNullException(nameof(parameter), "Cannot attach null parameter.");
			if (_reference == parameter) return;
			if (_reference != null && parameter.GetName() != _reference.GetName())
				throw new InvalidOperationException("Cannot attach parameter with different name.");
			if (_lastValue != null)
				parameter.Set(_lastValue);
		}

		public override string GetKey()
			=> _reference.GetName();

		public override bool IsDirty()
			=> !Equals(_lastValue, GetValue());

		public override void SetDirty(bool dirty = true) {
			if (dirty) return;
			_lastValue = GetValue();
		}

		public override byte[] Serialize()
			=> _reference.Serialize();

		public override void Deserialize(byte[] data)
			=> _reference.Deserialize(data);

		public override object GetValue()
			=> _reference.Get();

		public override PropertyFlags GetFlags()
			=> PropertyFlags.None
				| (_reference.IsSyncable() ? PropertyFlags.Synced : PropertyFlags.None)
				| (_reference.IsSavable() ? PropertyFlags.Persistent : PropertyFlags.None);

		public override void SetValue(object value)
			=> _reference.Set(value);
	}
}