using System;
using Nox.Avatars;
using System.Collections.Generic;
using api.nox.relay.types.Instance;
using Buffer = Nox.CCK.Utils.Buffer;

namespace api.nox.relay.types.Avatar {
	public class AvatarChangedEvent : RelayInstanceResponse {
		public AvatarChangedResult Result;
		public string              Reason;
		public ushort              PlayerId;
		public IAvatarIdentifier   AvatarIdentifier;

		public bool IsError
			=> Result != AvatarChangedResult.Success;

		public override bool FromBuffer(Buffer buffer) {
			buffer.Goto(0);
			InternalId = buffer.ReadByte();
			Result     = buffer.ReadEnum<AvatarChangedResult>();
			PlayerId   = buffer.ReadUShort();
			switch (Result) {
				default:
				case AvatarChangedResult.Unknown:
				case AvatarChangedResult.Failed when buffer.Remaining > 2:
					Reason = buffer.ReadString();
					break;
				case AvatarChangedResult.Success: {
					var id      = buffer.ReadUInt();
					var server  = buffer.ReadString();
					var version = buffer.ReadUShort();
					var meta    = new Dictionary<string, string[]>();
					if (version != ushort.MaxValue) meta.Add("version", new[] { version.ToString() });
					AvatarIdentifier = Main.AvatarAPI.Make(id, meta, server);
					break;
				}
			}

			return true;
		}

		public static AvatarChangedEvent CreateUnknown(ushort id, byte iid, string reason)
			=> new() {
				InternalId   = iid,
				ConnectionId = id,
				Result       = AvatarChangedResult.Unknown,
				Reason       = reason
			};
	}
}