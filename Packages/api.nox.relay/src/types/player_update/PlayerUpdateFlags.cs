using System;

namespace api.nox.relay.types.PlayerUpdate {
	[Flags]
	public enum PlayerUpdateFlags : byte {
		None        = 0x00,
		DisplayName = 0x01,
		Flags       = 0x02,
		All         = DisplayName | Flags
	}
}