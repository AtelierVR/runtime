using Newtonsoft.Json.Linq;
using Nox.Avatars;
using Nox.CCK.Utils;

namespace api.nox.avatar.network {
	public class CreateAssetRequest : ICreateAssetRequest, INoxObject {
		private  uint   _id;
		internal ushort Version;
		internal string Engine;
		internal string Platform;
		private  string _url;
		private  string _hash;
		private  long   _size;

		public ICreateAssetRequest SetId(uint id) {
			_id = id;
			return this;
		}

		public ICreateAssetRequest SetVersion(ushort version) {
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

		public ICreateAssetRequest SetSize(long size) {
			_size = size;
			return this;
		}

		public uint GetId()
			=> _id;

		public ushort GetVersion()
			=> Version;

		public string GetEngine()
			=> Engine;

		public string GetPlatform()
			=> Platform;

		public string GetUrl()
			=> _url;

		public string GetHash()
			=> _hash;

		public long GetSize()
			=> _size;

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

		public static CreateAssetRequest FromBase(ICreateAssetRequest data)
			=> new() {
				_id      = data.GetId(),
				Version  = data.GetVersion(),
				Engine   = data.GetEngine(),
				Platform = data.GetPlatform(),
				_url     = data.GetUrl(),
				_hash    = data.GetHash(),
				_size    = data.GetSize()
			};
	}
}