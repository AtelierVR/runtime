using System;

namespace api.nox.relay.types.Authentication {
	public enum AuthenticationFlags : byte {
		None         = 0,
		UseIntegrity = 1 << 0,
		UseGuest     = 1 << 1
	}
}