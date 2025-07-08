namespace api.nox.relay.types.Authentication {
	public enum AuthenticationResult : byte {
		Success      = 0,
		InvalidToken = 1,
		MasterError  = 2,
		Blacklisted  = 3,
		Unknown      = 4
	}
}