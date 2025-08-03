using Nox.Sessions;
using Nox.Worlds;
using UnityEngine;

namespace api.nox.offline {
	public class RelayDimension : IDimension {
		public RelayDimension(int index, IScene scene, bool isActive) {
			_index        = index;
			_scene        = scene;
			_isActive     = isActive;
		}

		private          int       _index;
		private readonly IScene    _scene;
		private          bool      _isActive;

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