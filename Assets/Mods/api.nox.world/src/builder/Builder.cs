#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using Nox.CCK.Build;
using Nox.CCK.Utils;
using Nox.CCK.Worlds;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace api.nox.world.builder {
	public static class Builder {
		public static bool IsBuilding { get; private set; } = false;

		[MenuItem("Tools/World Builder/Build", priority = 100)]
		public static void BuildMenu()
			=> BuildMenuAsync().Forget();

		public async static UniTask BuildMenuAsync() {
			if (!SceneDescriptorExtension.TryGetDescriptor(SceneManager.GetActiveScene(), out MainSceneDescriptor descriptor)) {
				EditorUtility.DisplayDialog("Build Failed", "No valid main scene descriptor found in the current scene.", "OK");
				return;
			}

			const string path = "Assets/Builds/";
			if (!Directory.Exists(path)) {
				try {
					Directory.CreateDirectory(path);
				} catch (Exception e) {
					EditorUtility.DisplayDialog("Build Failed", $"Failed to create output directory: {e.Message}", "OK");
					return;
				}
			}

			var data = new BuildData {
				Descriptor = descriptor,
				ShowDialog = true,
				OutputPath = path,
				Target     = PlatformExtensions.GetCurrentTarget()
			};

			var res = await Build(data);

			if (res.Type == BuildResultType.Success) {
				EditorUtility.DisplayDialog("Build Success", "The world has been built successfully!", "OK");
			} else EditorUtility.DisplayDialog("Build Failed", res.Message, "OK");
		}

		public static async UniTask<BuildResult> Build(BuildData data) {
			if (IsBuilding)
				return new BuildResult {
					Type    = BuildResultType.AlreadyBuilding,
					Message = "A build is already in progress."
				};

			if (EditorApplication.isCompiling)
				return new BuildResult {
					Type    = BuildResultType.EditorCompiling,
					Message = "Unity is currently compiling scripts. Please wait until the compilation is complete."
				};
			if (EditorApplication.isPlaying)
				return new BuildResult {
					Type    = BuildResultType.EditorPlaying,
					Message = "Unity is currently in play mode. Please stop playing before building."
				};

			if (data.Target == BuildTarget.NoTarget)
				return new BuildResult {
					Type    = BuildResultType.InvalidTarget,
					Message = "Invalid build target specified. Please select a valid build target."
				};

			if (data.Target == BuildTarget.NoTarget)
				data.Target = PlatformExtensions.GetCurrentTarget();

			if (!data.Target.IsSupported())
				return new BuildResult {
					Type    = BuildResultType.UnsupportedTarget,
					Message = $"The build target {data.Target} is not supported."
				};

			IsBuilding = true;
			var rollback = EditorSceneManager.GetSceneManagerSetup();

			var mainScene = data.Descriptor.gameObject.scene;
			if (!mainScene.IsValid()) {
				IsBuilding = false;
				EditorSceneManager.RestoreSceneManagerSetup(rollback);
				return new BuildResult {
					Type    = BuildResultType.InvalidScene,
					Message = "The scene is not valid. Please ensure the scene is properly set up."
				};
			}

			if (!EditorSceneManager.SaveOpenScenes()) {
				IsBuilding = false;
				EditorSceneManager.RestoreSceneManagerSetup(rollback);
				return new BuildResult {
					Type    = BuildResultType.Failed,
					Message = "Failed to save open scenes. Please ensure all scenes are saved before building."
				};
			}

			AssetDatabase.Refresh();

			var sceneAssets = data.Descriptor.EstimateScenes();

			var loadedScenes = new List<Scene> {
				EditorSceneManager.OpenScene(
					AssetDatabase.GetAssetPath(sceneAssets.First().Value),
					OpenSceneMode.Single
				)
			};
			foreach (var scene in sceneAssets.Skip(1)) {
				await UniTask.Yield();
				var loadedScene = EditorSceneManager.OpenScene(
					AssetDatabase.GetAssetPath(scene.Value),
					OpenSceneMode.Additive
				);
				if (loadedScene.IsValid())
					loadedScenes.Add(loadedScene);
			}

			if (loadedScenes.Count == 0) {
				IsBuilding = false;
				EditorSceneManager.RestoreSceneManagerSetup(rollback);
				return new BuildResult {
					Type    = BuildResultType.InvalidScene,
					Message = "No valid scenes found to build. Please ensure the main scene and sub-scenes are set up correctly."
				};
			}

			// compile all ICompilable scripts
			var compilableScripts = loadedScenes
				.SelectMany(scene => scene.GetRootGameObjects())
				.SelectMany(rootObject => rootObject.GetComponentsInChildren<ICompilable>(true))
				.OrderBy(script => script.CompileOrder)
				.ToList();

			foreach (var script in compilableScripts)
				try {
					Logger.LogDebug($"Compiling script: {script.GetType().Name} (Order: {script.CompileOrder})");
					script.Compile();
					await script.CompileAsync();
				} catch (Exception e) {
					IsBuilding = false;
					EditorSceneManager.RestoreSceneManagerSetup(rollback);
					return new BuildResult {
						Type    = BuildResultType.Failed,
						Message = $"Failed to compile script {script.GetType().Name}: {e.Message}"
					};
				}

			if (!EditorSceneManager.SaveOpenScenes()) {
				IsBuilding = false;
				EditorSceneManager.RestoreSceneManagerSetup(rollback);
				return new BuildResult {
					Type    = BuildResultType.Failed,
					Message = "Failed to save open scenes after compilation. Please ensure all scenes are saved before building."
				};
			}

			const string tempPath = "Assets/Temp/";
			const string depPath  = tempPath + "Dependencies/";
			if (Directory.Exists(tempPath))
				try {
					Directory.Delete(tempPath, true);
				} catch (Exception e) {
					IsBuilding = false;
					EditorSceneManager.RestoreSceneManagerSetup(rollback);
					return new BuildResult {
						Type    = BuildResultType.Failed,
						Message = $"Failed to delete temporary directory: {e.Message}"
					};
				}

			Directory.CreateDirectory(tempPath);
			if (!Directory.Exists(depPath))
				Directory.CreateDirectory(depPath);

			var assets       = new List<string>();
			var scenes       = new List<string>();
			var initialGUIDs = new Dictionary<string, string>();
			var endGUIDs     = new Dictionary<string, string>();

			foreach (var scene in sceneAssets) {
				var path        = AssetDatabase.GetAssetPath(scene.Value);
				var destination = tempPath + scene.Key + ".unity";
				if (string.IsNullOrEmpty(path) || !File.Exists(path)) {
					IsBuilding = false;
					EditorSceneManager.RestoreSceneManagerSetup(rollback);
					return new BuildResult {
						Type    = BuildResultType.InvalidScene,
						Message = $"Scene {scene.Value.name} is not valid or does not exist at path {path}."
					};
				}

				File.Copy(path, destination);
				File.Copy(path + ".meta", destination + ".meta");
				Logger.Log($"Copied {scene.Value.name} from {path} -> {destination}");
				assets.Add(destination);
				scenes.Add(destination);
				initialGUIDs.Add(destination, AssetDatabase.AssetPathToGUID(path));
				endGUIDs.Add(destination, Guid.NewGuid().ToString("N"));
				foreach (var dependency in AssetDatabase.GetDependencies(path)) {
					var gui             = Guid.NewGuid().ToString("N");
					var destinationPath = depPath + gui + Path.GetExtension(dependency);
					if (assets.Contains(destinationPath)) continue;
					switch (Path.GetExtension(dependency)) {
						case ".cs":
						case ".dll":
						case ".meta":
						case ".unity":
							continue;
					}

					File.Copy(dependency, destinationPath);
					File.Copy(dependency + ".meta", destinationPath + ".meta");
					Logger.Log("Copied " + dependency               + " to " + destinationPath);
					assets.Add(destinationPath);
					initialGUIDs.Add(destinationPath, AssetDatabase.AssetPathToGUID(dependency));
					endGUIDs.Add(destinationPath, gui);
				}
			}

			foreach (var asset in assets) {
				if (new[] {
					".unity", ".prefab", ".asset",
					".mat", ".anim", ".controller"
				}.Contains(Path.GetExtension(asset))) {
					var text = await File.ReadAllTextAsync(asset);
					text = initialGUIDs.Aggregate(
						text,
						(current, guid) => new Regex(guid.Value).Replace(current, endGUIDs[guid.Key])
					);
					await File.WriteAllTextAsync(asset, text);
				}

				var meta = await File.ReadAllTextAsync(asset + ".meta");
				meta = initialGUIDs.Aggregate(
					meta,
					(current, guid) => new Regex(guid.Value).Replace(current, endGUIDs[guid.Key])
				);
				await File.WriteAllTextAsync(asset + ".meta", meta);
			}

			AssetDatabase.Refresh();

			// get all new uids
			var newGUIDs = new Dictionary<string, string>();
			foreach (var asset in assets)
				if (asset.StartsWith(depPath)) {
					var fileName = Path.GetFileNameWithoutExtension(asset);
					newGUIDs.Add(fileName, AssetDatabase.AssetPathToGUID(asset));
					Logger.Log("Dependency: " + fileName + " -> " + newGUIDs[fileName]);
				}

			// set updated uids
			foreach (var asset in assets) {
				if (!new[] {
					".unity", ".prefab", ".asset",
					".mat", ".anim", ".controller"
				}.Contains(Path.GetExtension(asset))) continue;
				var text = await File.ReadAllTextAsync(asset);
				text = newGUIDs.Aggregate(text, (current, guid) => new Regex(guid.Key).Replace(current, guid.Value));
				await File.WriteAllTextAsync(asset, text);
			}

			AssetDatabase.Refresh();

			loadedScenes = new List<Scene> { EditorSceneManager.OpenScene(scenes[0], OpenSceneMode.Single) };
			foreach (var scene in scenes.Skip(1)) {
				await UniTask.Yield();
				var loadedScene = EditorSceneManager.OpenScene(scene, OpenSceneMode.Additive);
				if (loadedScene.IsValid())
					loadedScenes.Add(loadedScene);
			}

			IsBuilding = false;
			EditorSceneManager.RestoreSceneManagerSetup(rollback);
			return new BuildResult {
				Type = BuildResultType.Success,
			};
		}
	}
}
#endif