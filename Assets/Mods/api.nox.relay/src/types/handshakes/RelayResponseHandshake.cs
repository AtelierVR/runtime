using System.Net;
using Nox.CCK.Utils;

namespace api.nox.relay.types.Handshakes {
	public class ResponseHandshake : Response {
		public ushort       Protocol;
		public ushort       ClientId;
		public ClientStatus Status;
		public IPEndPoint   Address;
		public RelayFlags   Flags;
		public string       MasterAddress;

		public override bool FromBuffer(Buffer buffer) {
			buffer.Goto(0);
			if (buffer.length != 11) return false;
			Protocol = buffer.ReadUShort();
			ClientId = buffer.ReadUShort();
			Status   = buffer.ReadEnum<ClientStatus>();
			var address = buffer.ReadBytes(4);
			var port    = buffer.ReadUShort();
			Address = new IPEndPoint(new IPAddress(address), port);
			Flags   = buffer.ReadEnum<RelayFlags>();
			if (Flags.HasFlag(RelayFlags.AsMaster))
				MasterAddress = buffer.ReadString();
			return true;
		}

		public override string ToString()
			=> $"{GetType().Name}[Protocol={Protocol}, ClientId={ClientId}, Status={Status}, address={Address}]";
	}
}