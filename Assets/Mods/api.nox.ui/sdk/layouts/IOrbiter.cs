namespace Nox.UI {
	public interface IOrbiter {
		public bool    GetActive();
		public void    SetActive(bool active);
		public IPart[] GetParts();
	}
}