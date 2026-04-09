namespace Nox.Servers {
	public static class IServerExtensions {
		public static string GetAddress(this IServer server)
			=> server?.Address;

		public static string GetTitle(this IServer server)
			=> server?.Metadata?.Title;

		public static string GetDescription(this IServer server)
			=> server?.Metadata?.Description;

		public static string GetIconUrl(this IServer server)
			=> server?.Metadata?.Icon;
	}
}
