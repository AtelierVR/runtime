using api.nox.relay.types.Instance;
using Buffer = Nox.CCK.Utils.Buffer;

namespace api.nox.relay.types.Event {
	public class EventEvent : RelayInstanceResponse {
		public ushort SenderId;
		public string Name;
		public byte[] Payload;


		public override bool FromBuffer(Buffer buffer) {
			buffer.Goto(0);

			InternalId = buffer.ReadByte();
			SenderId   = buffer.ReadUShort();
			Name       = buffer.ReadString();
			var length = buffer.ReadUShort();
			Payload = buffer.ReadBytes(length);

			return true;
		}

		public override string ToString()
			=> $"{GetType().Name}[ConnectionId={ConnectionId}, InternalId={InternalId}, SenderId={SenderId}, Name={Name}, PayloadLength={Payload.Length}]";
	}
}