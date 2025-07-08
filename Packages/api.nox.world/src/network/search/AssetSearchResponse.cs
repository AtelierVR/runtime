using System;
using System.Linq;
using Nox.CCK.Utils;
using Nox.Worlds;

namespace api.nox.world.network {
	[Serializable]
	public class AssetSearchResponse : IAssetSearchResponse, INoxObject {
		public uint         total;
		public uint         limit;
		public uint         offset;
		public WorldAsset[] assets;

		public uint GetTotal()
			=> total;

		public uint GetLimit()
			=> limit;

		public uint GetOffset()
			=> offset;

		public WorldAsset[] GetInternalAssets()
			=> assets ?? Array.Empty<WorldAsset>();

		public IWorldAsset[] GetAssets()
			=> GetInternalAssets().Cast<IWorldAsset>().ToArray();
	}
}