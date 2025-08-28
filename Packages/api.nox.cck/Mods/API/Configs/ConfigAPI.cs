namespace Nox.CCK.Mods.Configs {
	public interface IConfigAPI {
		/// <summary>
		/// Returns the folder path where the config files of the mod are stored.
		/// <remarks>When the method is called, the folder is guaranteed to exist.</remarks>
		/// </summary>
		/// <returns></returns>
		public string GetFolder();

		/// <summary>
		/// Deletes all files and the folder where the config files of the mod are stored.
		/// </summary>
		public void ClearFolder();
	}
}