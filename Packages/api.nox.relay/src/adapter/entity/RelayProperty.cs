using Nox.Entities;

namespace api.nox.relay {
	public abstract class RelayProperty : IProperty {
		public abstract string GetKey();

		public abstract PropertyFlags GetFlags();

		public abstract object GetValue();

		public abstract void SetValue(object value);

		public abstract byte[] Serialize();

		public abstract void Deserialize(byte[] data);

		public abstract bool IsDirty();

		public abstract void SetDirty(bool dirty = true);
	}
}