namespace Nox.Worlds {
	public interface ICreateAssetRequest {
		/// <summary>
		/// Set the id of the asset.
		/// If the id is 0, the asset will use a generated id.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public ICreateAssetRequest SetId(uint id);

		/// <summary>
		/// Set the version of the asset.
		/// This is used to determine if the asset is up to date.
		/// </summary>
		/// <param name="version"></param>
		/// <returns></returns>
		public ICreateAssetRequest SetVersion(uint version);

		/// <summary>
		/// Set the engine of the asset.
		/// This is used to determine if the asset is compatible with the current engine.
		/// </summary>
		/// <param name="engine"></param>
		/// <returns></returns>
		public ICreateAssetRequest SetEngine(string engine);

		/// <summary>
		/// Set the platform of the asset.
		/// This is used to determine if the asset is compatible with the current platform.
		/// </summary>
		/// <param name="platform"></param>
		/// <returns></returns>
		public ICreateAssetRequest SetPlatform(string platform);

		/// <summary>
		/// Set the URL of the asset.
		/// This is the custom URL where the asset can be downloaded from.
		/// If the URL is not set, the asset will be empty and will not be downloaded.
		/// </summary>
		/// <param name="url"></param>
		/// <returns></returns>
		public ICreateAssetRequest SetUrl(string url);

		/// <summary>
		/// Set the hash (Sha256) of the asset.
		/// This is used to verify the integrity of the asset.
		/// If it is not set, the asset will be empty and will not be downloaded.
		/// </summary>
		/// <param name="hash"></param>
		/// <returns></returns>
		public ICreateAssetRequest SetHash(string hash);

		/// <summary>
		/// Set the size of the asset in bytes.
		/// This is used to determine the size of the asset when downloading it.
		/// If it is 0, the asset will be empty and will not be downloaded.
		/// </summary>
		/// <param name="size"></param>
		/// <returns></returns>
		public ICreateAssetRequest SetSize(uint size);
	}
}