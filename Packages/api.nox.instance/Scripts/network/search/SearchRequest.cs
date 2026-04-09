using System.Collections.Generic;
using Nox.CCK.Utils;
using Nox.Users;
using Nox.Worlds;
using ISearchRequest = Nox.Instances.ISearchRequest;

namespace api.nox.instance.network {
	public class SearchRequest : ISearchRequest, INoxObject {
		internal string Query;
		internal Identifier World = Identifier.Invalid;
		internal Identifier Owner = Identifier.Invalid;
		internal uint Offset;
		internal uint Limit;

		public string ToParams() {
			var text = "";
			if (!string.IsNullOrEmpty(Query))
				text += (text.Length > 0 ? "&" : "") + $"query={Query}";
			if (World.IsValid())
				text += (text.Length > 0 ? "&" : "") + $"world={World.ToString()}";
			if (Owner.IsValid())
				text += (text.Length > 0 ? "&" : "") + $"owner={Owner.ToString()}";
			if (Offset > 0)
				text += (text.Length > 0 ? "&" : "") + $"offset={Offset}";
			if (Limit > 0)
				text += (text.Length > 0 ? "&" : "") + $"limit={Limit}";
			return text;
		}

		public static SearchRequest From(Dictionary<string, object> data) {
			var req = new SearchRequest();
			if (data.TryGetValue("query", out var query) && query is string q)
				req.Query = q;
			if (data.TryGetValue("owner", out var owners) && owners is Identifier n)
				req.Owner = n;
			if (data.TryGetValue("world", out var worlds) && worlds is Identifier w)
				req.World = w;
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

		public ISearchRequest SetOwner(Identifier owner) {
			Owner = owner;
			return this;
		}

		public ISearchRequest SetWorld(Identifier world) {
			World = world;
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

		public Identifier GetOwner()
			=> Owner;

		public Identifier GetWorld()
			=> World;

		public uint GetOffset()
			=> Offset;

		public uint GetLimit()
			=> Limit;

		public static SearchRequest FromBase(ISearchRequest request) {
			if (request is SearchRequest sr)
				return sr;
			return new SearchRequest {
				Query  = request.GetQuery(),
				World  = request.GetWorld(),
				Owner  = request.GetOwner(),
				Offset = request.GetOffset(),
				Limit  = request.GetLimit()
			};
		}
	}
}