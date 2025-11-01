namespace Nox.Entities {
	[System.Flags]
	public enum PropertyFlags {
		None       = 0,
		LocalEmit  = 1 << 0,
		RemoteEmit = 1 << 1,
		Synced     = LocalEmit | RemoteEmit,
	}
}