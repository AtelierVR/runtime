using System;
using System.Linq;
using api.nox.instance.network;
using Nox.Search;

namespace api.nox.instance.search {
	public class SearchResult : IResult {
		public string         Error;
		public SearchResponse Response;
		public string         ServerAddress;
		public int            MenuId;

		public bool IsError()
			=> !string.IsNullOrEmpty(Error);

		public string GetError()
			=> Error;

		public bool HasNext()
			=> !IsError() && Response.HasNext();

		public IResultData[] GetData()
			=> Response != null
				? Response.instances
					.Select(x => new SearchData { Reference = x })
					.Cast<IResultData>()
					.ToArray()
				: Array.Empty<IResultData>();
	}
}