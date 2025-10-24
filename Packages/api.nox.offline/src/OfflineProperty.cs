using Nox.Entities;

namespace api.nox.offline {
	public class OfflineProperty : IProperty {
		public bool          IsDirty() {
			throw new System.NotImplementedException();
		}
		public void          SetDirty(bool dirty = true) {
			throw new System.NotImplementedException();
		}
		public byte[]        Serialize() {
			throw new System.NotImplementedException();
		}
		public void          Deserialize(byte[] data) {
			throw new System.NotImplementedException();
		}
		public string        GetKey() {
			throw new System.NotImplementedException();
		}
		public object        GetValue() {
			throw new System.NotImplementedException();
		}
		public PropertyFlags GetFlags() {
			throw new System.NotImplementedException();
		}
		public void          SetValue(object value) {
			throw new System.NotImplementedException();
		}
	}
}