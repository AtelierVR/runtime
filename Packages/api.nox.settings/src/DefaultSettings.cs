using System.Linq;
using api.nox.settings.handlers;
using Nox.Settings;

namespace api.nox.settings {
	public static class DefaultSettings {
		public static IHandler[] GetHandlers()
			=> new IHandler[] {
				new Language(),
				new Brightness(),
				new BloomIntensity(),
				new Quality(),
				new AntiAliasing(),
				new Resolution(),
				new WindowSize(),
				new ModalTesting()
			};

		public static (string, string, string) Split(this IHandler handler) {
			var path = handler.GetPath();
			return path.Length switch {
				0 => default,
				1 => ("general", "general", path[0]),
				2 => (path[0], "general", path[1]),
				_ => (path[0], path[1], string.Join("/", path.Skip(2)))
			};
		}

		public static string ToID(this IHandler handler)
			=> string.Join(":", handler.Split());

		private static string FormatLanguage(string text)
			=> text.ToLower().Trim().Replace(" ", "_");
	}
}