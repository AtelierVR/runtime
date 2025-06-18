using Newtonsoft.Json.Linq;
using Nox.CCK.Utils;
using Nox.Worlds;

namespace api.nox.world.network {
	[System.Serializable]
	public class UpdateWorldRequest : INoxObject, IUpdateWorldRequest {
		public string title       = "";
		public string description = "";
		public ushort capacity    = ushort.MaxValue;
		public string thumbnail   = "";

		public IUpdateWorldRequest SetTitle(string t) {
			title = t;
			return this;
		}

		public IUpdateWorldRequest SetDescription(string d) {
			description = d;
			return this;
		}

		public IUpdateWorldRequest SetCapacity(ushort c) {
			capacity = c;
			return this;
		}

		public IUpdateWorldRequest SetThumbnail(string i) {
			thumbnail = i;
			return this;
		}

		public string ToJson() {
			var obj = new JObject();

			if (title == null)
				obj["title"] = JValue.CreateNull();
			else if (title.Length > 0)
				obj["title"] = JValue.CreateString(title);

			if (description == null)
				obj["description"] = JValue.CreateNull();
			else if (description.Length > 0)
				obj["description"] = JValue.CreateString(description);

			if (capacity == ushort.MaxValue)
				obj["capacity"]  = JValue.CreateNull();
			else obj["capacity"] = capacity;

			if (thumbnail == null)
				obj["thumbnail"] = JValue.CreateNull();
			else if (thumbnail.Length > 0)
				obj["thumbnail"] = JValue.CreateString(thumbnail);
			
			return obj.ToString();
		}
	}
}