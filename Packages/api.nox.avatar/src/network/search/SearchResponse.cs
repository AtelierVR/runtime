using Newtonsoft.Json;
using Nox.CCK.Utils;

namespace api.nox.avatar.network {
    [System.Serializable]
    public class SearchResponse : INoxObject {
        [JsonProperty("avatars")]
        public Avatar[] Avatars { get; set; } = new Avatar[0];

        [JsonProperty("total")]
        public int Total { get; set; }

        [JsonProperty("search")]
        public string Search { get; set; }

        [JsonProperty("ids")]
        public int[] Ids { get; set; } = new int[0];

        [JsonProperty("limit")]
        public int Limit { get; set; }

        [JsonProperty("offset")]
        public int Offset { get; set; }

        public SearchResponse() { }

        public string ToJson() {
            return JsonConvert.SerializeObject(this);
        }

        public static SearchResponse FromJson(string json) {
            try {
                return JsonConvert.DeserializeObject<SearchResponse>(json);
            } catch {
                return null;
            }
        }
    }
}
