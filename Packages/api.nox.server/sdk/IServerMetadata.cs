namespace Nox.Servers {
	public interface IServerMetadata {
		/// <summary>Instance display name.</summary>
		public string Title { get; }

		/// <summary>Instance description, or null.</summary>
		public string Description { get; }

		/// <summary>URL to the instance icon, or null.</summary>
		public string Icon { get; }

		/// <summary>Contact info (email or URL), or null.</summary>
		public string Contact { get; }
	}
}
