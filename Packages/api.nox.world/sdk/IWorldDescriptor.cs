using UnityEngine;

namespace Nox.Worlds {
	public interface IWorldDescriptor {
		public GameObject GetAnchor();

		public T[] GetModules<T>() where T : IWorldModule;

		public IWorldModule[] GetModules();

		public IWorldModule[] FindModules();
	}
}