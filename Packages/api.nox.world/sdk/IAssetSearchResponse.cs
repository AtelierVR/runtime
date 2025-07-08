namespace Nox.Worlds {
	public interface IAssetSearchResponse {
		/// <summary>
		/// Gets the total number of assets that match the search criteria.
		/// </summary>
		/// <returns></returns>
		public uint GetTotal();

		/// <summary>
		/// Gets the limit of assets returned in this response.
		/// </summary>
		/// <returns></returns>
		public uint GetLimit();

		/// <summary>
		/// Gets the offset of the assets returned in this response.
		/// </summary>
		/// <returns></returns>
		public uint GetOffset();

		/// <summary>
		/// Gets the assets that match the search criteria at the specified offset and limit.
		/// </summary>
		/// <returns></returns>
		public IWorldAsset[] GetAssets();
	}
}