namespace api.nox.relay.types.Handshakes {
	public enum HandshakeFlags : byte {
		None      = 0,
		IsOffline = 1 << 0,
	}
}