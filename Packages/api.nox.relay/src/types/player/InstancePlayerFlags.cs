using System;

namespace api.nox.relay.types.Player {
	[Flags]
	public enum InstancePlayerFlags : uint {
		None              = 0,
		IsBot             = 1 << 0,
		InstanceMaster    = 1 << 1,
		InstanceModerator = 1 << 2,
		InstanceOwner     = 1 << 3,
		GuildModerator    = 1 << 4,
		MasterModerator   = 1 << 5,
		WorldOwner        = 1 << 6,
		WorldModerator    = 1 << 7,
		AuthUnverified    = 1 << 8,
		HideInList        = 1 << 9,

		IsOwner     = InstanceOwner     | WorldOwner,
		IsModerator = InstanceModerator | GuildModerator | MasterModerator | WorldModerator,
	}
}