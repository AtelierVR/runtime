namespace Nox.CCK.Network {
	public enum DirtyBy : byte {
		None   = 0,
		Local  = 1,             // used to notify local changes and need to push to others
		Remote = 2,             // used to notify remote changes and should apply to local
		Force  = Remote | Local // used to force apply changes locally, and apply to local and remote
	}
}