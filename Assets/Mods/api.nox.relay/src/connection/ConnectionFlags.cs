namespace api.nox.relay.connection {
	public enum ConnectionFlags : uint {
		None                   = 0,
		IsPublic               = 1 << 0,
		IsDefault              = 1 << 1,
		UsePassword            = 1 << 2,
		UseWhitelist           = 1 << 3,
		AuthorizeBot           = 1 << 4,
		UseMods                = 1 << 5,
		EnableCrossInventory   = 1 << 6,
		EnableCustomAvatar     = 1 << 7,
		AllowWorldModification = 1 << 8,
		AllowFly               = 1 << 9,
		AllowProps             = 1 << 10,
		VanishBlocked          = 1 << 11,
		GhostBlocked           = 1 << 12,
		GroupModeration        = 1 << 13
	}
}