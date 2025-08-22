using Newtonsoft.Json.Linq;
using Nox.CCK.Utils;

namespace api.nox.avatar.network {
    [System.Serializable]
    public class UpdateAvatarRequest : INoxObject {
        public string title = "";
        public string description = "";
        public string thumbnail = "";

        public UpdateAvatarRequest SetTitle(string t) {
            title = t;
            return this;
        }

        public UpdateAvatarRequest SetDescription(string d) {
            description = d;
            return this;
        }

        public UpdateAvatarRequest SetThumbnail(string t) {
            thumbnail = t;
            return this;
        }

        public string GetTitle() => title;
        public string GetDescription() => description;
        public string GetThumbnail() => thumbnail;

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

            if (thumbnail == null)
                obj["thumbnail"] = JValue.CreateNull();
            else if (thumbnail.Length > 0)
                obj["thumbnail"] = JValue.CreateString(thumbnail);

            return obj.ToString();
        }
    }
}
