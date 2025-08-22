using Newtonsoft.Json.Linq;
using Nox.CCK.Utils;

namespace api.nox.avatar.network {
	public class CreateAssetRequest : INoxObject {
		private  uint   _id;
		internal ushort Version;
		internal string Engine;
		internal string Platform;
		private  string _url;
		private  string _hash;
		private  long   _size;

		public CreateAssetRequest SetId(uint i) {
			_id = i;
			return this;
		}

		public CreateAssetRequest SetVersion(ushort v) {
			Version = v;
			return this;
		}

		public CreateAssetRequest SetEngine(string e) {
			Engine = e;
			return this;
		}

		public CreateAssetRequest SetPlatform(string p) {
			Platform = p;
			return this;
		}

		public CreateAssetRequest SetUrl(string u) {
			_url = u;
			return this;
		}

		public CreateAssetRequest SetHash(string h) {
			_hash = h;
			return this;
		}

		public CreateAssetRequest SetSize(long s) {
			_size = s;
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
			var obj = new JObject();

			if (_id > 0)
				obj["id"] = _id;

			obj["version"] = Version;

			if (!string.IsNullOrEmpty(Engine))
				obj["engine"] = Engine;

			if (!string.IsNullOrEmpty(Platform))
				obj["platform"] = Platform;

			if (!string.IsNullOrEmpty(_url))
				obj["url"] = _url;

			if (!string.IsNullOrEmpty(_hash))
				obj["hash"] = _hash;

			if (_size > 0)
				obj["size"] = _size;

			return obj.ToString();
		}
	}
}