using System;
using Nox.Avatars;
using System.Collections.Generic;
using api.nox.relay.types.Instance;
using Nox.CCK.Utils;
using Buffer = Nox.CCK.Utils.Buffer;

namespace api.nox.relay.types.Avatar {
	public class AvatarChangedEvent : RelayInstanceResponse {
		public AvatarChangedResult Result;
		public string              Reason;
		public ushort              PlayerId;
		public IAvatarIdentifier   AvatarIdentifier;

		public bool IsError
			=> Result != AvatarChangedResult.Changing;

		public bool IsSuccess
			=> Result == AvatarChangedResult.Success;

		public override bool FromBuffer(Buffer buffer) {
			Logger.LogDebug($"Received {buffer}");
			buffer.Goto(0);
			InternalId = buffer.ReadByte();
			Result     = buffer.ReadEnum<AvatarChangedResult>();
			switch (Result) {
				case AvatarChangedResult.Success:
					break;
				case AvatarChangedResult.Changing: {
					PlayerId = buffer.ReadUShort();
					var id      = buffer.ReadUInt();
					var server  = buffer.ReadString();
					var version = buffer.ReadUShort();
					var meta    = new Dictionary<string, string[]>();
					if (version != ushort.MaxValue) meta.Add("version", new[] { version.ToString() });
					AvatarIdentifier = Main.AvatarAPI.Make(id, meta, server);
					break;
				}
				case AvatarChangedResult.Unknown:
				case AvatarChangedResult.Failed:
				default:
					if (buffer.Remaining > 2)
						Reason = buffer.ReadString();
					break;
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

		public override string ToString()
			=> $"{GetType().Name}[InternalId={InternalId}, Result={Result}"
				+ (IsError ? $", Reason={Reason}" : "")
				+ (Result == AvatarChangedResult.Changing ? $", PlayerId={PlayerId}, AvatarIdentifier={AvatarIdentifier?.ToString()}" : "")
				+ "]";
	}
}