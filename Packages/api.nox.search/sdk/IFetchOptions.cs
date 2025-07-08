namespace Nox.Search {
	public interface IFetchOptions {
		public string GetQuery();
		public uint   GetPage();
		public uint   GetLimit();
		public int    GetMenuId();
	}
}