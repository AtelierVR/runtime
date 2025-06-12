using Nox.Search;

namespace api.nox.world.search {
	public class Search {
		private readonly IHandler _handler;

		internal Search() {
			_handler = Main.Instance.SearchAPI.Add(new SearchHandler());
		}

		internal void Dispose() {
			if (_handler == null) return;
			Main.Instance.SearchAPI.Remove(_handler.GetId());
		}
	}
}