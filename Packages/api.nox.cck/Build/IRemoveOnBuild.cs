namespace Nox.CCK.Build {
	/// <summary>
	/// Interface for components that should be removed during the build process.
	/// </summary>
	public interface IRemoveOnBuild {
		/// <summary>
		/// Called when the component is being removed during the build process (optional).
		/// </summary>
		void OnRemoveOnBuild() {}
	}
}