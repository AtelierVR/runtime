using System;
using Nox.Users;
using Buffer = Nox.CCK.Utils.Buffer;

namespace api.nox.relay.types.Authentication {
	public class RelayResponseAuthentication : RelayResponse {
		public AuthenticationResult Result;
		public string               Reason;

		public IUserIdentifier Identifier;
		public string          Display;
		public DateTime        ExpireAt;
		public byte[]          Challenge;

		public static RelayResponseAuthentication CreateUnknown(ushort id, string reason)
			=> new() {
				ConnectionId = id,
				Result       = AuthenticationResult.Unknown,
				Reason       = reason
			};

		public override bool FromBuffer(Buffer buffer) {
			buffer.Goto(0);
			Result = buffer.ReadEnum<AuthenticationResult>();
			switch (Result) {
				case AuthenticationResult.Challenge:
					Challenge = buffer.ReadBytes(buffer.ReadByte());
					return true;
				case AuthenticationResult.Invalid:
				case AuthenticationResult.MasterError:
				case AuthenticationResult.Signature:
				case AuthenticationResult.Unknown:
					Reason = buffer.Remaining >= 2
						? buffer.ReadString()
						: "Unknown error";
					return true;
				case AuthenticationResult.Success:
					Identifier = Main.UserAPI.Make(
						buffer.ReadUInt(),
						buffer.ReadString()
					);
					Display = buffer.ReadString();
					return true;
				case AuthenticationResult.Blacklisted:
					ExpireAt = buffer.ReadDateTime();
					Reason   = buffer.ReadString();
					return true;
				default:
					return false;
			}
		}

		public bool IsError()
			=> Result     != AuthenticationResult.Success
				&& Result != AuthenticationResult.Challenge;
	}
}