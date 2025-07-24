using System;
using Nox.CCK.Utils;
using Buffer = Nox.CCK.Utils.Buffer;

namespace api.nox.relay.types {
	public abstract class RelayResponse : INoxObject {
		public          ushort               ConnectionId;
		public          ushort               State;
		public          (DateTime, DateTime) Time;
		public abstract bool                 FromBuffer(Buffer buffer);
	}
}