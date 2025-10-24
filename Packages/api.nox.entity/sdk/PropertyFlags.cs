namespace Nox.Entities {
	[System.Flags]
	public enum PropertyFlags {
		None       = 0,
		Synced     = 1 << 1,
		Persistent = 1 << 2
	}
}