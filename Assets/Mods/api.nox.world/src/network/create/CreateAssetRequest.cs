using Newtonsoft.Json.Linq;
using Nox.CCK.Utils;
using Nox.Worlds;

namespace api.nox.world.network {
	[System.Serializable]
	public class CreateAssetRequest : INoxObject, ICreateAssetRequest {
		private  uint   _id;
		internal uint   Version;
		internal string Engine;
		internal string Platform;
		private  string _url;
		private  string _hash;
		private  uint   _size;

		public ICreateAssetRequest SetId(uint id) {
			_id = id;
			return this;
		}

		public ICreateAssetRequest SetVersion(uint version) {
			Version = version;
			return this;
		}

		public ICreateAssetRequest SetEngine(string engine) {
			Engine = engine;
			return this;
		}

		public ICreateAssetRequest SetPlatform(string platform) {
			Platform = platform;
			return this;
		}

		public ICreateAssetRequest SetUrl(string url) {
			_url = url;
			return this;
		}

		public ICreateAssetRequest SetHash(string hash) {
			_hash = hash;
			return this;
		}

		public ICreateAssetRequest SetSize(uint size) {
			_size = size;
			return this;
		}

		public string ToJson() {
			var obj = new JObject {
				["version"]  = Version,
				["engine"]   = Engine,
				["platform"] = Platform
			};

			if (_id   > 0) obj["id"]   = _id;
			if (_size > 0) obj["size"] = _size;

			if (!string.IsNullOrEmpty(Engine))
				obj["engine"] = Engine;

			if (!string.IsNullOrEmpty(Platform))
				obj["platform"] = Platform;

			if (!string.IsNullOrEmpty(_url))
				obj["url"] = _url;

			if (!string.IsNullOrEmpty(_hash))
				obj["hash"] = _hash;

			return obj.ToString();
		}
	}
}