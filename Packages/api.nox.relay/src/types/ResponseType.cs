namespace api.nox.relay.types {
	public enum ResponseType : byte {
		Disconnect       = 0x00,
		Handshake        = 0x01,
		Sessions         = 0x02,
		Latency          = 0x03,
		Authentication   = 0x04,
		Enter            = 0x05,
		Quit             = 0x06,
		CustomDataPacket = 0x07,
		Configuration    = 0x09,
		Join             = 0x0A,
		Leave            = 0x0B,
		Transform        = 0x0C,
		Teleport         = 0x0D,
		None             = 0xFF
	}
}