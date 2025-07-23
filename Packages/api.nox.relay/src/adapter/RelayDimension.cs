using Nox.Sessions;
using Nox.Worlds;

namespace api.nox.offline {
	public class RelayDimension : IDimension {
		public RelayDimension(string name, int index, IScene scene, bool isActive) {
			_name     = name;
			_index    = index;
			_scene    = scene;
			_isActive = isActive;
		}

		private readonly string _name;
		private          int    _index;
		private readonly IScene _scene;
		private          bool   _isActive;

		public string GetName()
			=> _name;

		public int GetMainIndex()
			=> _index;

		public IScene GetScene()
			=> _scene;

		public bool IsActive()
			=> _isActive;

		public void SetMainIndex(int index)
			=> _index = index;

		public void SetActive(bool isActive)
			=> _isActive = isActive;
	}
}