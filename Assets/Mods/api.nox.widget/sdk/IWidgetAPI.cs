namespace Nox.Widgets {
	public interface IWidgetAPI {
		public bool Has(string    key);
		public T    Get<T>(string key) where T : IWidget;
		public void Set(IWidget   widget);
		public void Remove(string key);
	}
}