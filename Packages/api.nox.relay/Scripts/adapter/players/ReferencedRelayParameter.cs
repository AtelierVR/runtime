using System;
using System.Linq;
using Nox.Avatars.Parameters;
using Nox.CCK.Network;
using Nox.CCK.Utils;
using Nox.Entities;

namespace api.nox.relay {
	public class ReferencedRelayParameter : RelayParameter {
		private readonly IParameter    _reference;
		private          object        _value;
		private          DirtyBy       _dirty;
		private readonly string        _name;
		private readonly int           _key;
		private readonly PropertyFlags _flags;

		public ReferencedRelayParameter(RelayEntity entity, IParameter parameter) : base(entity) {
			_reference = parameter ?? throw new ArgumentNullException(nameof(parameter), "Parameter cannot be null during construction.");
			_value     = _reference.Get();
			_name      = _reference.GetName();
			_key       = _reference.GetKey();
			_flags = PropertyFlags.None
				| (_reference.GetFlags().HasFlag(ParameterFlags.RemoteEditableByLocal) ? PropertyFlags.LocalEmit : PropertyFlags.None)
				| (_reference.GetFlags().HasFlag(ParameterFlags.LocalEditableByRemote) ? PropertyFlags.RemoteEmit : PropertyFlags.None);
		}


		private void OnValueChanged(object value, DirtyBy by) {
			_value = value;
			if (by == DirtyBy.Local)
				SetDirty(by);
		}

		public override int GetKey()
			=> _key;

		public override string GetName()
			=> _name;

		public override DirtyBy GetDirty()
			=> !IsValid() || AreValuesEqual(_reference.Get(), _value)
				? _dirty
				: DirtyBy.Local;

		private bool IsValid()
			=> _reference != null;

		public override void SetDirty(DirtyBy dirty) {
			_dirty = dirty switch {
				DirtyBy.Local                  => DirtyBy.Local,
				DirtyBy.None or DirtyBy.Remote => DirtyBy.None,
				_                              => throw new ArgumentOutOfRangeException(nameof(dirty), dirty, null)
			};

			if (!IsValid()) return;
			_value = _reference.Get();
		}

		/// <summary>
		/// Compare deux valeurs en tenant compte des types nullables et des collections
		/// </summary>
		private static bool AreValuesEqual(object value1, object value2) {
			if (ReferenceEquals(value1, value2))
				return true;

			if (value1 == null || value2 == null)
				return false;

			if (value1 is not byte[] bytes1 || value2 is not byte[] bytes2)
				return value1.Equals(value2);

			if (bytes1.Length != bytes2.Length)
				return false;

			return !bytes1.Where((t, i) => t != bytes2[i]).Any();
		}

		public override byte[] Serialize()
			=> (IsValid() ? _reference.Get() : _value)
				.ToBytes();

		public override void Deserialize(byte[] data, DirtyBy dirty)
			=> SetValue(data, dirty);

		public override object GetValue()
			=> IsValid()
				? _reference.Get()
				: _value;

		public override PropertyFlags GetFlags()
			=> _flags;

		public override void SetValue(object value, DirtyBy dirty) {
			if (!IsValid()) return;
			var old = _reference.Get().ToBool();
			_reference.Set(value);
			var @new = _reference.Get().ToBool();
			OnValueChanged(value, dirty);
		}

		public override string ToString()
			=> $"{GetType().Name}[Key={_name}, Value={GetValue()}, Flags={_flags}, Dirty={GetDirty()}]";
	}
}