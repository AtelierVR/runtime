using System.Collections.Generic;
using System.Linq;
using Nox.CCK.Utils;
using Nox.Worlds;

namespace Nox.CCK.Worlds {
	public class SearchRequest : ISearchRequest, INoxObject {
		public string Query;
		public uint[] Ids;
		public uint   Offset;
		public uint   Limit;

		public string ToParams() {
			var text = "";
			if (!string.IsNullOrEmpty(Query))
				text += (text.Length > 0 ? "&" : "") + $"query={Query}";
			if (Ids != null)
				text = Ids
					.Aggregate(text, (current, u) => current + (current.Length > 0 ? "&" : "") + $"id={u}");
			if (Offset > 0) text += (text.Length > 0 ? "&" : "") + $"offset={Offset}";
			if (Limit  > 0) text += (text.Length > 0 ? "&" : "") + $"limit={Limit}";
			return string.IsNullOrEmpty(text) ? "" : "?" + text;
		}

		public static SearchRequest From(Dictionary<string, object> data) {
			var req = new SearchRequest();
			if (data.TryGetValue("query", out var query) && query is string q)
				req.Query = q;
			if (data.TryGetValue("ids", out var userIds) && userIds is uint[] u)
				req.Ids = u;
			if (data.TryGetValue("offset", out var offset) && offset is uint o)
				req.Offset = o;
			if (data.TryGetValue("limit", out var limit) && limit is uint l)
				req.Limit = l;
			return req;
		}

		public ISearchRequest SetQuery(string query) {
			Query = query;
			return this;
		}

		public ISearchRequest SetIds(uint[] userIds) {
			Ids = userIds;
			return this;
		}

		public ISearchRequest SetOffset(uint offset) {
			Offset = offset;
			return this;
		}

		public ISearchRequest SetLimit(uint limit) {
			Limit = limit;
			return this;
		}

		public string GetQuery()
			=> Query;

		public uint[] GetIds()
			=> Ids;

		public uint GetOffset()
			=> Offset;

		public uint GetLimit()
			=> Limit;

		public static SearchRequest From(ISearchRequest request) {
			if (request is SearchRequest sr) return sr;
			var req = new SearchRequest {
				Query  = request.GetQuery(),
				Ids    = request.GetIds(),
				Offset = request.GetOffset(),
				Limit  = request.GetLimit()
			};
			return req;
		}
	}
}