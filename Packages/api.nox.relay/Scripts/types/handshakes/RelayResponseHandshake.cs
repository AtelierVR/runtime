using System.Net;
using Nox.CCK.Utils;

namespace api.nox.relay.types.Handshakes {
	public class RelayResponseHandshake : RelayResponse {
		public ushort         Protocol;
		public ushort         ClientId;
		public IPEndPoint     Address;
		public HandshakeFlags Flags;
		public string         MasterAddress;
		public ushort         MaxPacketSize;
		public ushort         ConnectionTimeout;
		public ushort         KeepAliveInterval;
		public ushort         SegmentationTimeout;

		public override bool FromBuffer(Buffer buffer) {
			buffer.Goto(0);
			Protocol = buffer.ReadUShort();
			ClientId = buffer.ReadUShort();
			var address = buffer.ReadBytes(4);
			var port    = buffer.ReadUShort();
			Address = new IPEndPoint(new IPAddress(address), port);
			Flags   = buffer.ReadEnum<HandshakeFlags>();
			if (!Flags.HasFlag(HandshakeFlags.IsOffline))
				MasterAddress = buffer.ReadString();
			
			// Read additional server configuration
			MaxPacketSize        = buffer.ReadUShort();
			ConnectionTimeout    = buffer.ReadUShort();
			KeepAliveInterval    = buffer.ReadUShort();
			SegmentationTimeout  = buffer.ReadUShort();
			
			return true;
		}

		public override string ToString()
			=> $"{GetType().Name}[Protocol={Protocol}, ClientId={ClientId}, address={Address}, MaxPacketSize={MaxPacketSize}]";
	}
}