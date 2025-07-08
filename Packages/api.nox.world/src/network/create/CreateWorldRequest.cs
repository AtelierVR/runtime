using Newtonsoft.Json.Linq;
using Nox.CCK.Utils;
using Nox.Worlds;

namespace api.nox.world.network {
	public class CreateWorldRequest : INoxObject, ICreateWorldRequest {
		internal uint   Id;
		private  string _title;
		private  string _description;
		private  ushort _capacity;
		private  string _thumbnail;

		public ICreateWorldRequest SetId(uint i) {
			Id = i;
			return this;
		}

		public ICreateWorldRequest SetTitle(string t) {
			_title = t;
			return this;
		}

		public ICreateWorldRequest SetDescription(string d) {
			_description = d;
			return this;
		}

		public ICreateWorldRequest SetCapacity(ushort c) {
			_capacity = c;
			return this;
		}

		public ICreateWorldRequest SetThumbnail(string t) {
			_thumbnail = t;
			return this;
		}

		public uint GetId()
			=> Id;

		public string GetTitle()
			=> _title;

		public string GetDescription()
			=> _description;

		public ushort GetCapacity()
			=> _capacity;

		public string GetThumbnail()
			=> _thumbnail;

		public string ToJson() {
			var obj = new JObject();

			if (Id > 0) obj["id"] = Id;

			if (!string.IsNullOrEmpty(_title))
				obj["title"] = _title;

			if (!string.IsNullOrEmpty(_description))
				obj["description"] = _description;

			obj["capacity"] = _capacity;

			if (!string.IsNullOrEmpty(_thumbnail))
				obj["thumbnail"] = _thumbnail;

			return obj.ToString();
		}

		public static CreateWorldRequest FromBase(ICreateWorldRequest data)
			=> new() {
				Id           = data.GetId(),
				_title       = data.GetTitle(),
				_description = data.GetDescription(),
				_capacity    = data.GetCapacity(),
				_thumbnail   = data.GetThumbnail()
			};
	}
}