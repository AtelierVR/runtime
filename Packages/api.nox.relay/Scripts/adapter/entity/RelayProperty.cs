using Nox.CCK.Network;
using Nox.Entities;

namespace api.nox.relay {
	public abstract class RelayProperty : IProperty {
		public RelayPlayer Owner;

		protected RelayProperty(RelayPlayer player)
			=> Owner = player;

		public abstract string GetKey();

		public abstract PropertyFlags GetFlags();

		public abstract object GetValue();

		public abstract void SetValue(object value, DirtyBy by);

		public abstract byte[] Serialize();

		public abstract void Deserialize(byte[] data, DirtyBy by);

		public abstract DirtyBy GetDirty();

		public abstract void SetDirty(DirtyBy dirtyBy);
	}
}