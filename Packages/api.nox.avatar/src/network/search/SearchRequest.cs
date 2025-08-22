using System.Collections.Generic;
using System.Text;
using Nox.CCK.Utils;

namespace api.nox.avatar.network {
    [System.Serializable]
    public class SearchRequest : INoxObject {
        public string search = "";
        public string[] tags = null;
        public string ids = "";
        public int limit = 10;
        public int offset = 0;

        public SearchRequest SetSearch(string s) {
            search = s;
            return this;
        }

        public SearchRequest SetTags(string[] t) {
            tags = t;
            return this;
        }

        public SearchRequest SetIds(string i) {
            ids = i;
            return this;
        }

        public SearchRequest SetLimit(int l) {
            limit = l;
            return this;
        }

        public SearchRequest SetOffset(int o) {
            offset = o;
            return this;
        }

        public string ToParams() {
            var parameters = new List<string>();

            if (!string.IsNullOrEmpty(search))
                parameters.Add($"search={System.Uri.EscapeDataString(search)}");

            if (tags != null && tags.Length > 0)
                parameters.Add($"tags={System.Uri.EscapeDataString(string.Join(",", tags))}");

            if (!string.IsNullOrEmpty(ids))
                parameters.Add($"ids={System.Uri.EscapeDataString(ids)}");

            if (limit != 10)
                parameters.Add($"limit={limit}");

            if (offset > 0)
                parameters.Add($"offset={offset}");

            return parameters.Count > 0 ? "?" + string.Join("&", parameters) : "";
        }

        public string ToJson() {
            return UnityEngine.JsonUtility.ToJson(this);
        }
    }
}
