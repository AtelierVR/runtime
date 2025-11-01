namespace Nox.Avatars.Parameters {
	[System.Flags]
	public enum ParameterFlags {
		None                  = 0,
		LocalEditable         = 1 << 0,
		RemoteEditable        = 1 << 1,
		LocalEditableByRemote = 1 << 3,
		RemoteEditableByLocal = 1 << 4,
		Persistent            = 1 << 2,
		Syncable              = LocalEditableByRemote | RemoteEditableByLocal,
		Editable              = LocalEditable         | RemoteEditable
	}
}