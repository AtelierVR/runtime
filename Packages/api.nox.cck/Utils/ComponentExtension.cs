using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityTransform = UnityEngine.Transform;
using NoxTransform = Nox.CCK.Utils.Transform;
using Object = UnityEngine.Object;

namespace Nox.CCK.Utils {
	public static class ComponentExtension {
		// ReSharper disable Unity.PerformanceAnalysis
		public static T GetOrAddComponent<T>(this Component component) where T : Component
			=> GetOrAddComponent<T>(component.gameObject);

		public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
			=> gameObject.GetComponent<T>() ?? gameObject.AddComponent<T>();

		public static bool IsActive(this GameObject gameObject)
			=> gameObject && gameObject.activeInHierarchy;

		public static bool IsActive(this Component component)
			=> component && IsActive(component.gameObject);

		public static T GetComponentInParents<T>(this GameObject gameObject, bool includeInactive = false) {
			var parent = gameObject.transform;

			while (parent) {
				if (!includeInactive && !parent.gameObject.activeInHierarchy)
					return default;
				if (parent.TryGetComponent<T>(out var component))
					return component;
				parent = parent.parent;
			}

			return default;
		}

		public static bool TryGetComponentInChildren<T>(out T component, bool includeInactive = true) {
			for (var i = 0; i < SceneManager.sceneCount; i++)
				if (TryGetComponentInChildren(SceneManager.GetSceneAt(i), out component, includeInactive))
					return true;
			component = default;
			return false;
		}

		public static bool TryGetComponentInChildren<T>(this Scene scene, out T component, bool includeInactive = true) {
			if (!scene.isLoaded) {
				component = default;
				return false;
			}

			foreach (var go in scene.GetRootGameObjects())
				if (go.TryGetComponentInChildren(out component, includeInactive))
					return true;

			component = default;
			return false;
		}

		public static bool TryGetComponentInChildren<T>(this GameObject gameObject, out T component, bool includeInactive = true) {
			if (!gameObject || (!includeInactive && !gameObject.activeInHierarchy)) {
				component = default;
				return false;
			}

			if (gameObject.TryGetComponent(out component))
				return true;

			foreach (UnityTransform child in gameObject.transform)
				if (TryGetComponentInChildren(child.gameObject, out component, includeInactive))
					return true;

			component = default;
			return false;
		}

		public static T GetComponentInChildren<T>(bool includeInactive = true)
			=> TryGetComponentInChildren(out T component, includeInactive)
				? component
				: default;

		public static T GetComponentInChildren<T>(this Scene scene, bool includeInactive = true)
			=> TryGetComponentInChildren(scene, out T component, includeInactive)
				? component
				: default;

		public static T[] GetComponentsInChildren<T>(this GameObject gameObject, bool includeInactive = true)
			=> gameObject.GetComponentsInChildren<T>(includeInactive);

		public static T GetComponentInChildren<T>(this GameObject gameObject, bool includeInactive = true)
			=> TryGetComponentInChildren(gameObject, out T component, includeInactive)
				? component
				: default;

		public static T[] GetComponentsInChildren<T>(bool includeInactive = true) {
			var components = new List<T>();
			for (var i = 0; i < SceneManager.sceneCount; i++)
				components.AddRange(GetComponentsInChildren<T>(SceneManager.GetSceneAt(i), includeInactive));
			return components.ToArray();
		}

		public static T[] GetComponentsInChildren<T>(this Scene scene, bool includeInactive = true) {
			if (!scene.isLoaded) return Array.Empty<T>();
			var components = new List<T>();
			foreach (var go in scene.GetRootGameObjects())
				components.AddRange(go.GetComponentsInChildren<T>(includeInactive));
			return components.ToArray();
		}

		public const char PathSeparator = '/';

		public static UnityTransform GetByPath(this Scene scene, string path)
			=> string.IsNullOrEmpty(path) ? null : scene.GetByPath(path.Split(PathSeparator));

		public static UnityTransform GetByPath(this UnityTransform transform, string path)
			=> string.IsNullOrEmpty(path) ? null : transform.GetByPath(path.Split(PathSeparator));

		public static UnityTransform GetByPath(this Scene scene, string[] path) {
			if (!scene.isLoaded || path.Length == 0)
				return null;
			return (from go in scene.GetRootGameObjects()
				where go.name == path[0]
				select go.transform.GetByPath(path.Skip(1).ToArray())).FirstOrDefault();
		}

		public static UnityTransform GetByPath(this UnityTransform transform, string[] path) {
			if (path.Length == 0)
				return transform;
			return (from UnityTransform child in transform
				where child.name == path[0]
				select child.GetByPath(path.Skip(1).ToArray())).FirstOrDefault();
		}

		public static void Move(this UnityTransform transform, NoxTransform move, float threshold = float.Epsilon) {
			if (!transform || move == null) return;

			if (!move.IsSamePosition(transform.position, threshold))
				transform.position = move.GetPosition();
			if (!move.IsSameRotation(transform.rotation, threshold))
				transform.rotation = move.GetRotation();
			if (!move.IsSameScale(transform.localScale, threshold))
				transform.localScale = move.GetScale();

			if (!transform.TryGetComponent<Rigidbody>(out var rb)) return;

			if (!move.IsSameVelocity(rb.linearVelocity, threshold))
				rb.linearVelocity = move.GetVelocity();
			if (!move.IsSameAngularVelocity(rb.angularVelocity, threshold))
				rb.angularVelocity = move.GetAngularVelocity();
		}

		public static void Destroy(this Object @object) {
			#if UNITY_EDITOR
			if (Application.isPlaying) Object.Destroy(@object);
			else UnityEditor.EditorApplication.delayCall += () => Object.DestroyImmediate(@object);
			#else
			Object.Destroy(@object);
			#endif
		}

		public static void DestroyImmediate(this Object @object) {
			#if UNITY_EDITOR
			if (Application.isPlaying) Object.DestroyImmediate(@object);
			else UnityEditor.EditorApplication.delayCall += () => Object.DestroyImmediate(@object);
			#else
			Object.DestroyImmediate(@object);
			#endif
		}

		public static GameObject Find(this GameObject parent, string name) {
			if (parent.name.Equals(name, StringComparison.OrdinalIgnoreCase))
				return parent;
			return (from UnityTransform child in parent.transform
				select Find(child.gameObject, name))
				.FirstOrDefault(result => result);
		}
	}
}