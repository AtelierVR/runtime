using System;
using Nox.CCK.Network;
using Nox.Entities;

namespace api.nox.offline {
	public class OfflineProperty : IProperty {
		public DirtyBy GetDirty()
			=> DirtyBy.None;

		public void SetDirty(DirtyBy dirty) { }

		public byte[] Serialize() {
			throw new System.NotImplementedException();
		}

		public void Deserialize(byte[] data, DirtyBy dirtyBy) {
			throw new System.NotImplementedException();
		}

		public int GetKey() {
			throw new System.NotImplementedException();
		}

		public DateTime GetUpdated()
			=> DateTime.UtcNow;

		public string GetName() {
			throw new System.NotImplementedException();
		}

		public object GetValue() {
			throw new System.NotImplementedException();
		}

		public PropertyFlags GetFlags() {
			throw new System.NotImplementedException();
		}

		public void SetValue(object value, DirtyBy dirtyBy) {
			throw new System.NotImplementedException();
		}
	}
}