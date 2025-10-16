using System;

namespace api.nox.relay.types.Authentication {
	public enum AuthenticationAction : byte
	{
		RequestChallenge = 0,
		ResolveChallenge = 1
	}
}