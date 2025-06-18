using System;
using Nox.CCK.Utils;

namespace api.nox.world.network {
	[Serializable]
	public class AssetSearchRequest : INoxObject {
		internal uint     offset;
		internal uint     limit;
		internal bool     show_empty;
		internal ushort[] versions;
		internal string[] engines;
		internal string[] platforms;

		public string ToParams() {
			var text             = "";
			if (offset > 0) text += (text.Length > 0 ? "&" : "") + $"offset={offset}";
			if (limit  > 0) text += (text.Length > 0 ? "&" : "") + $"limit={limit}";
			if (show_empty) text += (text.Length > 0 ? "&" : "") + "empty";
			if (versions != null)
				foreach (var v in versions)
					text += (text.Length > 0 ? "&" : "") + $"version={v}";
			if (engines != null)
				foreach (var e in engines)
					text += (text.Length > 0 ? "&" : "") + $"engine={e}";
			if (platforms != null)
				foreach (var p in platforms)
					text += (text.Length > 0 ? "&" : "") + $"platform={p}";
			return string.IsNullOrEmpty(text) ? "" : "?" + text;
		}

		public AssetSearchRequest SetOffset(uint offset) {
			this.offset = offset;
			return this;
		}

		public AssetSearchRequest SetLimit(uint limit) {
			this.limit = limit;
			return this;
		}

		public AssetSearchRequest SetShowEmpty(bool showEmpty) {
			show_empty = showEmpty;
			return this;
		}

		public AssetSearchRequest SetVersions(ushort[] versions) {
			this.versions = versions;
			return this;
		}

		public AssetSearchRequest SetEngines(string[] engines) {
			this.engines = engines;
			return this;
		}

		public AssetSearchRequest SetPlatforms(string[] platforms) {
			this.platforms = platforms;
			return this;
		}
	}
}