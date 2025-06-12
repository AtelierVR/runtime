using System.Collections.Generic;
using System.Linq;
using Nox.CCK.Worlds;
using UnityEditor;
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

		public static bool TryGetSceneDescriptor<T>(out T descriptor) where T : BaseSceneDescriptor {
			var mainScene = SceneManager.GetActiveScene();
			if (TryGetSceneDescriptor(mainScene, out descriptor))
				return true;
			for (var i = 0; i < SceneManager.sceneCount; i++) {
				var scene = SceneManager.GetSceneAt(i);
				if (scene.IsValid() && TryGetSceneDescriptor(scene, out descriptor))
					return true;
			}

			descriptor = null;
			return false;
		}

		public static bool TryGetSceneDescriptor<T>(Scene scene, out T descriptor) where T : BaseSceneDescriptor {
			if (!scene.IsValid() || !scene.isLoaded) {
				descriptor = null;
				return false;
			}

			var rootObjects = scene.GetRootGameObjects();
			foreach (var rootObject in rootObjects) {
				descriptor = rootObject.GetComponentInChildren<T>();
				if (descriptor)
					return true;
			}

			descriptor = null;
			return false;
		}
	}
}
#endif