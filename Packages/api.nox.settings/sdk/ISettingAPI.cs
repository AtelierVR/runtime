namespace Nox.Settings {
	public interface ISettingAPI {
		public IHandler Add(IHandler handler);

		public void Remove(string[] path);

		public IHandler Get(string[] path);

		public bool Has(string[] path);
	}
}