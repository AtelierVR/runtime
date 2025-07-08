namespace Nox.UI {
	public enum NavigationFlags {
		None        = 0,
		Enable      = 1 << 0,
		Interactive = 1 << 1,
		Button = Enable | Interactive
	}
}