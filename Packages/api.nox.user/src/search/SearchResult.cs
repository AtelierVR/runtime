using System;
using System.Linq;
using api.nox.user.network;
using Nox.Search;

namespace api.nox.user.search {

	public class SearchResult : IResult {
		public string         Error;
		public SearchResponse Response;
		public string         ServerAddress;

		public bool IsError()
			=> !string.IsNullOrEmpty(Error);

		public string GetError()
			=> Error;

		public bool HasNext()
			=> !IsError() && Response.HasNext();

		public IResultData[] GetData()
			=> Response != null
				? Response.users
					.Select(x => new SearchData { Reference = x })
					.Cast<IResultData>()
					.ToArray()
				: Array.Empty<IResultData>();
	}

}