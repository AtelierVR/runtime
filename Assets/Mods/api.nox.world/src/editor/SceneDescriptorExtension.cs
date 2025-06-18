using System.Collections.Generic;
using System.Linq;
using Nox.CCK.Worlds;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
namespace api.nox.world.editor {
	public static class SceneDescriptorExtension {
		public static Dictionary<byte, SceneAsset> EstimateScenes(this MainSceneDescriptor descriptor) {
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

		public static Dictionary<byte, GameObject> EstimateSpawns(this MainSceneDescriptor descriptor) {
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


		public static bool TryGetDescriptor<T>(GameObject o, out T descriptor) where T : BaseSceneDescriptor {
			if (o.TryGetComponent(out descriptor))
				return true;
			foreach (Transform child in o.transform)
				if (TryGetDescriptor(child.gameObject, out descriptor))
					return true;
			descriptor = null;
			return false;
		}

		public static bool TryGetDescriptor<T>(Scene scene, out T descriptor) where T : BaseSceneDescriptor {
			if (!scene.isLoaded) {
				descriptor = null;
				return false;
			}

			foreach (var go in scene.GetRootGameObjects())
				if (TryGetDescriptor(go, out descriptor))
					return true;
			descriptor = null;
			return false;
		}

		public static T[] GetDescriptors<T>() where T : BaseSceneDescriptor {
			var descriptors = new List<T>();
			for (var i = 0; i < SceneManager.sceneCount; i++)
				if (TryGetDescriptor(SceneManager.GetSceneAt(i), out T descriptor))
					descriptors.Add(descriptor);
			return descriptors.ToArray();
		}

		[MenuItem("Nox/Worlds/Make Main Scene Descriptor")]
		public static void MakeMainSceneDescriptor() {
			var selectedObjects = Selection.gameObjects;
			var descriptor = selectedObjects.Length > 0
				? MakeSceneDescriptor<MainSceneDescriptor>(selectedObjects[0])
				: MakeSceneDescriptor<MainSceneDescriptor>(SceneManager.GetActiveScene());
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

		public static T MakeSceneDescriptor<T>(GameObject selected) where T : BaseSceneDescriptor {
			var descriptor = new GameObject(typeof(T).Name);
			descriptor.transform.SetParent(selected.transform, false);
			var desc = descriptor.AddComponent<T>();
			descriptor.transform.localPosition = Vector3.zero;
			descriptor.transform.localRotation = Quaternion.identity;
			descriptor.transform.localScale    = Vector3.one;
			return desc;
		}

		public static T MakeSceneDescriptor<T>(Scene scene) where T : BaseSceneDescriptor {
			if (!scene.IsValid() || !scene.isLoaded) return null;
			var rootObjects = scene.GetRootGameObjects();
			if (rootObjects.Length == 0) return null;
			var desc = MakeSceneDescriptor<T>(rootObjects[0]);
			SceneManager.MoveGameObjectToScene(desc.gameObject, scene);
			return desc;
		}
	}
}
#endif