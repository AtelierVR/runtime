using System;
using Buffer = Nox.CCK.Utils.Buffer;

namespace api.nox.relay.types.Authentication {
	public class RelayResponseAuthentication : RelayResponse {
		public bool IsError
			=> Result is AuthenticationResult.Unknown
				or AuthenticationResult.MasterError
				or AuthenticationResult.InvalidToken
				or AuthenticationResult.Blacklisted;

		public AuthenticationResult Result;
		public DateTime             ExpireAt = DateTime.MinValue;
		public string               Reason;

		public bool HasExpiration
			=> ExpireAt != DateTime.MinValue;

		// Player information
		public uint   Id;
		public string Display;
		public string Address;

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
				case AuthenticationResult.Unknown or AuthenticationResult.MasterError:
					if (buffer.Remaining >= 2) // ushort of the string length of optional string
						Reason  = buffer.ReadString();
					else Reason = "Unknown error";
					return true;
				case AuthenticationResult.Success:
					Id      = buffer.ReadUShort();
					Display = buffer.ReadString();
					Address = buffer.ReadString();
					return true;
				case AuthenticationResult.Blacklisted:
					ExpireAt = buffer.ReadDateTime();
					Reason   = buffer.ReadString();
					return true;
				case AuthenticationResult.InvalidToken:
					Reason = "Invalid token";
					return true;
			}

			return false;
		}
	}
}