namespace api.nox.relay.types {

	public enum ResponseType : byte {
		None = 0xFF,

		// System Messages
		Disconnect   = 0x00,
		Handshake    = 0x01,
		Segmentation = 0x02,
		Reliable     = 0x03,
		Latency      = 0x04,

		Authentification    = 0x05,
		Enter               = 0x06,
		Quit                = 0x07,
		Custom              = 0x08,
		PasswordRequirement = 0x09,
		Traveling           = 0x0A,
		Transform           = 0x0B,
		Teleport            = 0x0C,
		AvatarChanged       = 0x0D,
		ServerConfig        = 0x0E,
		AvatarParams        = 0x0F,
		Join                = 0x10,
		Leave               = 0x11,
		Sessions              = 0x12,
	}
}