namespace api.nox.relay.types.Avatar {
	public enum AvatarChangedResult : byte {
		Success = 1 << 0,
		Unknown = 1 << 1,
		Failed  = 1 << 2
	}
}