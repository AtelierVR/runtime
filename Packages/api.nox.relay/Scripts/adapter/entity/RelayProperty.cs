using System;
using Nox.CCK.Network;
using Nox.Entities;
using UnityEngine;

namespace api.nox.relay {
	public abstract class RelayProperty : IProperty {
		public RelayEntity Owner;

		protected RelayProperty(RelayEntity entity)
			=> Owner = entity;

		public abstract int GetKey();
		
		public abstract string GetName();

		public abstract PropertyFlags GetFlags();

		public abstract object GetValue();
		
		public abstract DateTime GetUpdated();

		public abstract void SetValue(object value, DirtyBy by);

		public abstract byte[] Serialize();

		public abstract void Deserialize(byte[] data, DirtyBy by);

		public abstract DirtyBy GetDirty();

		public abstract void SetDirty(DirtyBy dirtyBy);
	}
}