namespace api.nox.relay.types.Authentication {
	public enum AuthenticationResult : byte {
		Success     = 0,
		Challenge   = 1,
		MasterError = 2,
		Blacklisted = 3,
		Invalid     = 4,
		Signature   = 5,
		Unknown     = 255
	}
}