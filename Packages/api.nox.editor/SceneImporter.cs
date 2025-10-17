#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.SceneManagement;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.Editor {
	public class SceneImporter : AssetPostprocessor {
		private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
			var scenes = EditorBuildSettings.scenes.ToList();
			var old    = EditorBuildSettings.scenes.ToList();

			foreach (var asset in importedAssets)
				if (asset.EndsWith(".unity")) {
					Logger.Log("Scene imported: " + asset);
					var scene = SceneManager.GetSceneByPath(asset);
					if (scenes.All(s => s.path != asset))
						scenes.Add(new EditorBuildSettingsScene(asset, true));
				}

			foreach (var asset in deletedAssets)
				if (asset.EndsWith(".unity")) {
					Logger.Log("Scene deleted: " + asset);
					scenes.RemoveAll(s => s.path == asset);
				}

			foreach (var asset in movedAssets)
				if (asset.EndsWith(".unity")) {
					Logger.Log("Scene moved: " + asset);
					var scene = SceneManager.GetSceneByPath(asset);
					scenes.RemoveAll(s => s.path == asset);
					scenes.Add(new EditorBuildSettingsScene(asset, true));
				}

			foreach (var asset in movedFromAssetPaths)
				if (asset.EndsWith(".unity")) {
					Logger.Log("Scene moved from: " + asset);
					scenes.RemoveAll(s => s.path == asset);
				}

			var newScenes = scenes.Distinct().ToArray();
			if (!old.Except(newScenes).Any() && !newScenes.Except(old).Any())
				return;
			
			EditorBuildSettings.scenes = newScenes;
			AssetDatabase.SaveAssets();
			Logger.Log("Updated scenes in build settings.");
		}

		[MenuItem("Nox/Scenes/Refresh Scenes in Build Settings"), InitializeOnLoadMethod]
		public static void RefreshScenesInBuildSettings() {
			var oldScenes = EditorBuildSettings.scenes;
			var newScenes = AssetDatabase.FindAssets("t:Scene")
				.Select(AssetDatabase.GUIDToAssetPath)
				.Select(path => new EditorBuildSettingsScene(path, true))
				.Distinct()
				.ToArray();

			if (!oldScenes.Except(newScenes).Any()) {
				Logger.Log("Scenes in build settings are already up to date.");
				return;
			}

			EditorBuildSettings.scenes = newScenes;
			AssetDatabase.SaveAssets();
			Logger.Log("Refreshed scenes in build settings.");
		}
	}
}
#endif