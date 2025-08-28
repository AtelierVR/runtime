using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nox.CCK.Utils {
	public static class ComponentExtension {
		// ReSharper disable Unity.PerformanceAnalysis
		public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
			=> gameObject.GetComponent<T>() ?? gameObject.AddComponent<T>();

		public static bool IsActive(this GameObject gameObject)
			=> gameObject && gameObject.activeInHierarchy;

		public static bool IsActive(this Component component)
			=> component && IsActive(component.gameObject);
		
		public static T GetComponentInParents<T>(this GameObject gameObject) {
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

		public static bool TryGetComponentInChildren<T>(out T component) {
			for (var i = 0; i < SceneManager.sceneCount; i++)
				if (TryGetComponentInChildren(SceneManager.GetSceneAt(i), out component))
					return true;
			component = default;
			return false;
		}

		public static bool TryGetComponentInChildren<T>(this Scene scene, out T component) {
			if (!scene.isLoaded) {
				component = default;
				return false;
			}

			foreach (var go in scene.GetRootGameObjects())
				if (go.TryGetComponentInChildren(out component))
					return true;

			component = default;
			return false;
		}

		public static bool TryGetComponentInChildren<T>(this GameObject gameObject, out T component) {
			if (gameObject.TryGetComponent(out component))
				return true;

			foreach (UnityEngine.Transform child in gameObject.transform)
				if (TryGetComponentInChildren(child.gameObject, out component))
					return true;

			component = default;
			return false;
		}

		public static T GetComponentInChildren<T>()
			=> TryGetComponentInChildren(out T component)
				? component
				: default;

		public static T GetComponentInChildren<T>(this Scene scene)
			=> TryGetComponentInChildren(scene, out T component)
				? component
				: default;

		public static T GetComponentInChildren<T>(this GameObject gameObject)
			=> TryGetComponentInChildren(gameObject, out T component)
				? component
				: default;

		public static T[] GetComponentsInChildren<T>() {
			var components = new List<T>();
			for (var i = 0; i < SceneManager.sceneCount; i++)
				components.AddRange(GetComponentsInChildren<T>(SceneManager.GetSceneAt(i)));
			return components.ToArray();
		}

		public static T[] GetComponentsInChildren<T>(Scene scene) {
			if (!scene.isLoaded) return Array.Empty<T>();
			var components = new List<T>();
			foreach (var go in scene.GetRootGameObjects())
				components.AddRange(go.GetComponentsInChildren<T>());
			return components.ToArray();
		}
	}
}