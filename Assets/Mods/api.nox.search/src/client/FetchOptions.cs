using Nox.Search;

namespace api.nox.search.client  {
	public class FetchOptions : IFetchOptions {
		public string Query;
		public uint   Page  = 0;
		public uint   Limit = 0;

		public string GetQuery()
			=> Query;

		public uint GetPage()
			=> Page;

		public uint GetLimit()
			=> Limit;
	}
}