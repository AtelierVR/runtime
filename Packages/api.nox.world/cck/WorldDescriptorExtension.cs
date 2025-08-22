using System;
using System.Collections.Generic;
using System.Linq;
using Nox.Worlds;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Nox.CCK.Worlds {
	public static class WorldDescriptorExtension {
		#if UNITY_EDITOR
		public static Dictionary<byte, SceneAsset> EstimateScenes(this MainWorldDescriptor descriptor) {
			var set = new HashSet<SceneAsset> { AssetDatabase.LoadAssetAtPath<SceneAsset>(descriptor.gameObject.scene.path) };
			foreach (var scenePath in descriptor.GetSceneAssets().Where(scenePath => scenePath))
				set.Add(scenePath);
			var scenesDict = new Dictionary<byte, SceneAsset>();
			for (byte i = 0; i < set.Count; i++) {
				var estimatedId = i;
				while (scenesDict.ContainsKey(estimatedId)) estimatedId++;
				scenesDict.Add(estimatedId, set.ToArray()[i]);
			}

			return scenesDict;
		}
		#endif

		public static Dictionary<byte, GameObject> EstimateSpawns(this BaseWorldDescriptor descriptor) {
			var set = new HashSet<GameObject>();
			foreach (var spawn in descriptor.GetSpawns().Where(spawn => spawn))
				set.Add(spawn);
			if (set.Count == 0) set.Add(descriptor.gameObject);
			var spawnsDict = new Dictionary<byte, GameObject>();
			for (byte i = 0; i < set.Count; i++) {
				var estimatedId = i;
				while (spawnsDict.ContainsKey(estimatedId)) estimatedId++;
				spawnsDict.Add(estimatedId, set.ToArray()[i]);
			}

			return spawnsDict;
		}


		public static bool TryGetDescriptor<T>(GameObject o, out T descriptor) where T : IBaseWorldDescriptor {
			if (o.TryGetComponent(out descriptor))
				return true;
			foreach (Transform child in o.transform)
				if (TryGetDescriptor(child.gameObject, out descriptor))
					return true;
			descriptor = default;
			return false;
		}

		public static bool TryGetDescriptor<T>(Scene scene, out T descriptor) where T : IBaseWorldDescriptor {
			if (!scene.isLoaded) {
				descriptor = default;
				return false;
			}

			foreach (var go in scene.GetRootGameObjects())
				if (TryGetDescriptor(go, out descriptor))
					return true;
			descriptor = default;
			return false;
		}

		public static T[] GetDescriptors<T>() where T : IBaseWorldDescriptor {
			var descriptors = new List<T>();
			for (var i = 0; i < SceneManager.sceneCount; i++)
				if (TryGetDescriptor(SceneManager.GetSceneAt(i), out T descriptor))
					descriptors.Add(descriptor);
			return descriptors.ToArray();
		}

		#if UNITY_EDITOR
		[MenuItem("Nox/Worlds/Make Main Scene Descriptor")]
		public static void MakeMainSceneDescriptor() {
			var selectedObjects = Selection.gameObjects;
			var descriptor = selectedObjects.Length > 0
				? MakeSceneDescriptor<MainWorldDescriptor>(selectedObjects[0])
				: MakeSceneDescriptor<MainWorldDescriptor>(SceneManager.GetActiveScene());
			if (descriptor) {
				Selection.activeGameObject = descriptor.gameObject;
				EditorGUIUtility.PingObject(descriptor.gameObject);
			} else
				EditorUtility.DisplayDialog(
					"Error",
					"Failed to create MainSceneDescriptor. Please ensure you have a valid scene selected.",
					"OK"
				);
		}
		#endif

		public static T MakeSceneDescriptor<T>(GameObject selected) where T : BaseWorldDescriptor {
			var descriptor = new GameObject(typeof(T).Name);
			descriptor.transform.SetParent(selected.transform, false);
			var desc = descriptor.AddComponent<T>();
			descriptor.transform.localPosition = Vector3.zero;
			descriptor.transform.localRotation = Quaternion.identity;
			descriptor.transform.localScale    = Vector3.one;
			return desc;
		}

		public static T MakeSceneDescriptor<T>(Scene scene) where T : BaseWorldDescriptor {
			if (!scene.IsValid() || !scene.isLoaded) return null;
			var rootObjects = scene.GetRootGameObjects();
			if (rootObjects.Length == 0) return null;
			var desc = MakeSceneDescriptor<T>(rootObjects[0]);
			SceneManager.MoveGameObjectToScene(desc.gameObject, scene);
			return desc;
		}

		public static bool UseSpawn(this IBaseWorldDescriptor descriptor)
			=> descriptor.GetSpawnType() != SpawnType.None;

		public static GameObject ChoiceSpawn(this IBaseWorldDescriptor descriptor, Vector3[] occupied = null) {
			var spawns = descriptor.GetSpawns();
			if (spawns.Length == 0) return descriptor.GetRoot();
			if (spawns.Length == 1) return spawns[0];
			occupied ??= Array.Empty<Vector3>();
			var type = descriptor.GetSpawnType();
			if (type == SpawnType.None)
				type = SpawnType.Random;

			return type switch {
				SpawnType.Sequential => GetSequentialSpawn(spawns, descriptor),
				SpawnType.Random     => spawns[UnityEngine.Random.Range(0, spawns.Length)],
				SpawnType.Select     => spawns[descriptor.GetSpawnIndex()],
				SpawnType.Free       => GetFreeSpawn(spawns, occupied),
				_                    => spawns[0]
			};
		}

		private static GameObject GetSequentialSpawn(GameObject[] spawns, IBaseWorldDescriptor descriptor)
			=> spawns[descriptor.NextSpawnIndex()];
		
		private static GameObject GetFreeSpawn(GameObject[] spawns, Vector3[] occupied) {
			var bestSpawn = spawns[0];
			if (occupied.Length == 0)
				return spawns[UnityEngine.Random.Range(0, spawns.Length)];
			var maxMinDistance = 0f;
			foreach (var spawn in spawns) {
				var minDistanceToOccupied = occupied
					.Select(occupiedPoint => Vector3.Distance(spawn.transform.position, occupiedPoint))
					.Prepend(float.MaxValue)
					.Min();
				if (!(minDistanceToOccupied > maxMinDistance)) continue;
				maxMinDistance = minDistanceToOccupied;
				bestSpawn      = spawn;
			}

			return bestSpawn;
		}
	}
}