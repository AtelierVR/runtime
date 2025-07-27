using System;
using api.nox.relay.types.Instance;
using api.nox.relay.types.Player;
using Buffer = Nox.CCK.Utils.Buffer;

namespace api.nox.relay.types.Enter {
	public class EnterResponse : RelayInstanceResponse {
		public EnterResult    Result;
		public DateTime       ExpireAt = DateTime.MinValue;
		public string         Reason;
		public InstancePlayer Player;
		public byte           Tps;
		public float          Threshold;

		public bool HasExpiration
			=> ExpireAt != DateTime.MinValue;

		public bool IsError
			=> Result is not EnterResult.Success;

		public static EnterResponse CreateUnknown(ushort id, byte iid, string reason)
			=> new() {
				ConnectionId = id,
				InternalId   = iid,
				Result       = EnterResult.Unknown,
				Reason       = reason
			};

		public override bool FromBuffer(Buffer buffer) {
			InternalId = buffer.ReadByte();
			Result     = buffer.ReadEnum<EnterResult>();
			switch (Result) {
				case EnterResult.Unknown:
					if (buffer.Remaining >= 2) // ushort of the string length of optional string
						Reason  = buffer.ReadString();
					else Reason = "Unknown error";
					return true;
				case EnterResult.Success:
					Player = new InstancePlayer {
						ConnectionId = ConnectionId,
						InternalId   = InternalId,
						Flags        = buffer.ReadEnum<InstancePlayerFlags>(),
						Id           = buffer.ReadUShort(),
						Identifier   = Main.UserAPI.Make(buffer.ReadUInt(), buffer.ReadString()),
						Display      = buffer.ReadString(),
						CreatedAt    = buffer.ReadDateTime(),
					};
					Tps = buffer.ReadByte();
					Threshold = buffer.ReadFloat();
					return true;
				case EnterResult.Blacklisted:
					ExpireAt = buffer.ReadDateTime();
					Reason   = buffer.ReadString();
					return true;
			}

			return false;
		}

		public override string ToString()
			=> $"{GetType().Name}[Result={Result}, Player={Player?.ToString() ?? "null"}, MaxTps={Tps}]";
	}
}