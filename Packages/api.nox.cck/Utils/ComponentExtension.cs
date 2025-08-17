namespace Nox.CCK.Utils {
	public static class ComponentExtension {
		// ReSharper disable Unity.PerformanceAnalysis
		public static T GetOrAddComponent<T>(this UnityEngine.GameObject gameObject) where T : UnityEngine.Component
			=> gameObject.GetComponent<T>() ?? gameObject.AddComponent<T>();

		public static T GetComponentInParents<T>(this UnityEngine.GameObject gameObject) {
			if (gameObject.TryGetComponent<T>(out var component))
				return component;

			var parent = gameObject.transform.parent;

			while (parent) {
				if (parent.TryGetComponent(out component))
					return component;
				parent = parent.parent;
			}

			return default;
		}
	}
}