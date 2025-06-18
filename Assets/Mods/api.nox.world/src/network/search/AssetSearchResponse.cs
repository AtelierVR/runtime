using System;
using Nox.CCK.Utils;

namespace api.nox.world.network {
	[Serializable]
	public class AssetSearchResponse : INoxObject {
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

		public WorldAsset[] GetAssets()
			=> assets ?? Array.Empty<WorldAsset>();
	}
}
