using Newtonsoft.Json.Linq;
using Nox.Avatars;
using Nox.CCK.Utils;

namespace api.nox.avatar.network {
	public class CreateAvatarRequest : ICreateAvatarRequest, INoxObject {
		internal uint   Id;
		private  string _title;
		private  string _description;
		private  string _thumbnail;

		public ICreateAvatarRequest SetId(uint i) {
			Id = i;
			return this;
		}

		public ICreateAvatarRequest SetTitle(string t) {
			_title = t;
			return this;
		}

		public ICreateAvatarRequest SetDescription(string d) {
			_description = d;
			return this;
		}

		public ICreateAvatarRequest SetThumbnail(string t) {
			_thumbnail = t;
			return this;
		}

		public uint GetId()
			=> Id;

		public string GetTitle()
			=> _title;

		public string GetDescription()
			=> _description;

		public string GetThumbnail()
			=> _thumbnail;

		public string ToJson() {
			var obj = new JObject();

			if (Id > 0) obj["id"] = Id;

			if (!string.IsNullOrEmpty(_title))
				obj["title"] = _title;

			if (!string.IsNullOrEmpty(_description))
				obj["description"] = _description;

			if (!string.IsNullOrEmpty(_thumbnail))
				obj["thumbnail"] = _thumbnail;

			return obj.ToString();
		}

		public static CreateAvatarRequest FromBase(ICreateAvatarRequest data)
			=> new() {
				Id           = data.GetId(),
				_title       = data.GetTitle(),
				_description = data.GetDescription(),
				_thumbnail   = data.GetThumbnail()
			};
	}
}