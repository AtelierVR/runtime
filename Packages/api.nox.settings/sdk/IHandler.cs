namespace Nox.Settings {
	public interface ISettingHandler {
		public void OnSelected();

		public void OnDeselected();

		public ISettingPage[] GetPages();
	}
}