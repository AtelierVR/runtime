using Nox.Sessions;
using Nox.Worlds;
using UnityEngine;

namespace api.nox.offline {
	public class RelayDimension : IDimension {
		public RelayDimension(int index, IRuntimeWorld runtimeWorld, bool isActive) {
			_index        = index;
			_runtimeWorld = runtimeWorld;
			_isActive     = isActive;
		}

		private          int           _index;
		private readonly IRuntimeWorld _runtimeWorld;
		private          bool          _isActive;

		public int GetMainIndex()
			=> _index;

		public IRuntimeWorld GetScene()
			=> _runtimeWorld;

		public bool IsActive()
			=> _isActive;

		public void SetMainIndex(int index)
			=> _index = index;

		public void SetActive(bool isActive)
			=> _isActive = isActive;

		public IWorldDescriptor GetDescriptor(int index) {
			var instances = _runtimeWorld.GetInstances();
			if (index < 0 || index >= instances.Length) return null;
			var main = instances[index];
			return main.GetDescriptor(index == 0 ? GetMainIndex() : throw new System.NotImplementedException());
		}

		public GameObject GetAnchor(int index) {
			var instances = _runtimeWorld.GetInstances();
			if (index < 0 || index >= instances.Length) return null;
			return instances[index].GetAnchor(index == 0 ? GetMainIndex() : throw new System.NotImplementedException());
		}

		public bool IsLoaded(int index) {
			var instances = _runtimeWorld.GetInstances();
			return index >= 0 && index < instances.Length;
		}

		public int GetSize()
			=> _runtimeWorld.GetInstances().Length;
	}
}